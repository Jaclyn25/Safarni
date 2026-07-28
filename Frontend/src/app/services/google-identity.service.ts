import { Injectable } from '@angular/core';

export interface GoogleCredentialResponse {
  credential: string;
  select_by?: string;
  clientId?: string;
}

export interface GoogleButtonOptions {
  type?: 'standard' | 'icon';
  theme?: 'outline' | 'filled_blue' | 'filled_black';
  size?: 'large' | 'medium' | 'small';
  text?:
    | 'signin_with'
    | 'signup_with'
    | 'continue_with'
    | 'signin';
  shape?: 'rectangular' | 'pill' | 'circle' | 'square';
  logo_alignment?: 'left' | 'center';
  width?: number;
  locale?: string;
}

interface GoogleIdConfiguration {
  client_id: string;
  callback: (
    response: GoogleCredentialResponse
  ) => void;
  ux_mode?: 'popup' | 'redirect';
  auto_select?: boolean;
  cancel_on_tap_outside?: boolean;
}

interface GoogleAccountsIdApi {
  initialize(
    configuration: GoogleIdConfiguration
  ): void;

  renderButton(
    parent: HTMLElement,
    options: GoogleButtonOptions
  ): void;

  disableAutoSelect(): void;
}

interface GoogleIdentityNamespace {
  accounts: {
    id: GoogleAccountsIdApi;
  };
}

declare global {
  interface Window {
    google?: GoogleIdentityNamespace;
  }
}

@Injectable({
  providedIn: 'root'
})
export class GoogleIdentityService {
  private readonly scriptId =
    'google-identity-services-script';

  private scriptLoadPromise:
    Promise<GoogleAccountsIdApi> | null = null;

  async renderButton(
    container: HTMLElement,
    clientId: string,
    callback: (
      response: GoogleCredentialResponse
    ) => void
  ): Promise<void> {
    if (!clientId.trim()) {
      throw new Error(
        'Google client ID is not configured.'
      );
    }

    const googleId = await this.loadClient();

    googleId.initialize({
      client_id: clientId,
      callback,
      ux_mode: 'popup',
      auto_select: false,
      cancel_on_tap_outside: true
    });

    /*
     * Prevent duplicate Google buttons when Angular
     * reuses or re-renders the component.
     */
    container.replaceChildren();

    const measuredWidth = Math.floor(
      container.getBoundingClientRect().width
    );

    const buttonWidth = Math.min(
      400,
      Math.max(240, measuredWidth || 400)
    );

    googleId.renderButton(container, {
      type: 'standard',
      theme: 'outline',
      size: 'large',
      text: 'continue_with',
      shape: 'rectangular',
      logo_alignment: 'left',
      width: buttonWidth
    });
  }

  disableAutoSelect(): void {
    window.google?.accounts.id.disableAutoSelect();
  }

  private loadClient():
    Promise<GoogleAccountsIdApi> {
    const loadedApi =
      window.google?.accounts.id;

    if (loadedApi) {
      return Promise.resolve(loadedApi);
    }

    if (this.scriptLoadPromise) {
      return this.scriptLoadPromise;
    }

    this.scriptLoadPromise =
      new Promise<GoogleAccountsIdApi>(
        (resolve, reject) => {
          const waitForApi = (
            attempt = 0
          ): void => {
            const api =
              window.google?.accounts.id;

            if (api) {
              resolve(api);
              return;
            }

            /*
             * Wait up to five seconds for the Google
             * script to expose its global API.
             */
            if (attempt >= 50) {
              reject(
                new Error(
                  'Google Identity Services loaded without its API.'
                )
              );
              return;
            }

            window.setTimeout(
              () => waitForApi(attempt + 1),
              100
            );
          };

          const existingScript =
            document.getElementById(
              this.scriptId
            ) as HTMLScriptElement | null;

          if (existingScript) {
            waitForApi();
            return;
          }

          const script =
            document.createElement('script');

          script.id = this.scriptId;
          script.src =
            'https://accounts.google.com/gsi/client';
          script.async = true;
          script.defer = true;

          script.onload = () => {
            waitForApi();
          };

          script.onerror = () => {
            this.scriptLoadPromise = null;

            reject(
              new Error(
                'Google Identity Services could not be loaded.'
              )
            );
          };

          document.head.appendChild(script);
        }
      ).catch((error: unknown) => {
        this.scriptLoadPromise = null;
        throw error;
      });

    return this.scriptLoadPromise;
  }
}