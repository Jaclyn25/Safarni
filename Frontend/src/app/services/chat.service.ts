import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { timeout } from 'rxjs/operators';

export interface ChatMessage {
  id?: string;
  role: string;
  content: string;
  publicTrips?: any[]; // Allow public trips in message
}

export interface Conversation {
  id: string;
  title: string;
  createdAt: string;
  isPinned?: boolean;
  folderId?: string | null;
  messages: ChatMessage[];
}

export interface ChatStreamHandlers {
  onToken: (token: string) => void;
  onStatus?: (status: string) => void;
  onResources?: (sources: ChatSource[], places: ChatPlaceCard[]) => void;
  onCompleted: (conversation: Conversation) => void;
}

export interface ChatSource {
  title: string;
  url: string;
  domain: string;
  kind: 'knowledge' | 'place';
}

export interface ChatPlaceCard {
  name: string;
  address: string;
  rating: number | null;
  reviewCount: number | null;
  url: string;
  description: string | null;
}

interface ChatStreamEvent {
  type: 'token' | 'status' | 'resources' | 'completed' | 'error';
  token: string | null;
  conversation: Conversation | null;
  code: string | null;
  status: string | null;
  sources: ChatSource[] | null;
  places: ChatPlaceCard[] | null;
}

class ChatStreamRequestError extends Error {
  constructor(
    message: string,
    readonly status: number
  ) {
    super(message);
    this.name = 'ChatStreamRequestError';
  }
}

export interface ChatFolder {
  id: string;
  name: string;
  createdAt: string;
  conversations: Conversation[];
}

@Injectable({
  providedIn: 'root'
})
export class ChatService {
  private readonly apiUrl = '/api/Chat';
  private readonly requestTimeoutMs = 20000;
  private readonly responseTimeoutMs = 75000;

  constructor(private http: HttpClient) { }

  private getHeaders(): HttpHeaders {
    const token = localStorage.getItem('access_token');
    let headers = new HttpHeaders({
      'Content-Type': 'application/json'
    });
    if (token) {
      headers = headers.set('Authorization', `Bearer ${token}`);
    }
    return headers;
  }

  startConversation(title?: string): Observable<Conversation> {
    return this.http
      .post<Conversation>(`${this.apiUrl}/conversations`, { title }, { headers: this.getHeaders() })
      .pipe(timeout(this.requestTimeoutMs));
  }

  getConversations(): Observable<Conversation[]> {
    return this.http
      .get<Conversation[]>(`${this.apiUrl}/conversations`, { headers: this.getHeaders() })
      .pipe(timeout(this.requestTimeoutMs));
  }

  getConversationById(id: string): Observable<Conversation> {
    return this.http
      .get<Conversation>(`${this.apiUrl}/conversations/${id}`, { headers: this.getHeaders() })
      .pipe(timeout(this.requestTimeoutMs));
  }

  planTrip(prompt: string): Observable<any> {
    return this.http
      .post<any>('/api/ai/plan-trip', { prompt }, { headers: this.getHeaders() })
      .pipe(timeout(this.requestTimeoutMs));
  }

  async streamMessage(
    conversationId: string,
    message: string,
    handlers: ChatStreamHandlers,
    signal?: AbortSignal
  ): Promise<void> {
    const response = await fetch(`${this.apiUrl}/stream`, {
      method: 'POST',
      headers: this.getFetchHeaders(),
      body: JSON.stringify({
        conversationId,
        message
      }),
      signal
    });

    if (!response.ok) {
      throw new ChatStreamRequestError(
        await this.readApiError(response),
        response.status
      );
    }

    if (!response.body) {
      throw new Error(
        'The server did not return a streaming response.'
      );
    }

    const reader = response.body.getReader();
    const decoder = new TextDecoder();

    let buffer = '';
    let completed = false;

    while (true) {
      const { value, done } = await reader.read();

      buffer += decoder
        .decode(value, { stream: !done })
        .replace(/\r\n/g, '\n');

      let boundaryIndex = buffer.indexOf('\n\n');

      while (boundaryIndex >= 0) {
        const block = buffer
          .slice(0, boundaryIndex)
          .trim();

        buffer = buffer.slice(boundaryIndex + 2);

        if (block) {
          completed =
            this.handleStreamBlock(block, handlers) ||
            completed;
        }

        boundaryIndex = buffer.indexOf('\n\n');
      }

      if (!done) {
        continue;
      }

      const remainingBlock = buffer.trim();

      if (remainingBlock) {
        completed =
          this.handleStreamBlock(
            remainingBlock,
            handlers
          ) || completed;
      }

      if (!completed) {
        throw new Error(
          'The assistant response ended before it was saved.'
        );
      }

      return;
    }
  }

  regenerateResponse(conversationId: string): Observable<Conversation> {
    return this.http
      .post<Conversation>(`${this.apiUrl}/regenerate`, { conversationId }, { headers: this.getHeaders() })
      .pipe(timeout(this.responseTimeoutMs));
  }

  editMessage(conversationId: string, messageId: string, newContent: string): Observable<Conversation> {
    return this.http.post<Conversation>(`${this.apiUrl}/edit`, { conversationId, messageId, newContent }, { headers: this.getHeaders() });
  }

  deleteConversation(id: string): Observable<{ success: boolean }> {
    return this.http
      .delete<{ success: boolean }>(`${this.apiUrl}/conversations/${id}`, { headers: this.getHeaders() })
      .pipe(timeout(this.requestTimeoutMs));
  }

  private getFetchHeaders(): Record<string, string> {
    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
      Accept: 'text/event-stream'
    };

    const token = localStorage.getItem('access_token');

    if (token) {
      headers['Authorization'] = `Bearer ${token}`;
    }

    return headers;
  }

  private handleStreamBlock(
    block: string,
    handlers: ChatStreamHandlers
  ): boolean {
    const event = this.parseStreamEvent(block);
    const data =
      event.data as Partial<ChatStreamEvent> | null;

    if (event.name === 'token' && data?.token) {
      handlers.onToken(data.token);
      return false;
    }

    if (event.name === 'status' && data?.status) {
      handlers.onStatus?.(data.status);
      return false;
    }

    if (event.name === 'resources') {
      handlers.onResources?.(data?.sources ?? [], data?.places ?? []);
      return false;
    }

    if (
      event.name === 'completed' &&
      data?.conversation
    ) {
      handlers.onCompleted(data.conversation);
      return true;
    }

    if (event.name === 'error') {
      if (data?.code === 'Chat.SessionLimitReached') {
        throw new ChatStreamRequestError(
          data.token ||
            'Start a new chat to continue.',
          429
        );
      }

      throw new Error(
        data?.token ||
          'The assistant could not complete the response.'
      );
    }

    return false;
  }

  private parseStreamEvent(
    block: string
  ): { name: string; data: unknown } {
    const lines = block.split('\n');

    const name =
      lines
        .find((line) => line.startsWith('event:'))
        ?.slice(6)
        .trim() ?? 'message';

    const dataText = lines
      .filter((line) => line.startsWith('data:'))
      .map((line) => line.slice(5).trimStart())
      .join('\n');

    try {
      return {
        name,
        data: dataText
          ? (JSON.parse(dataText) as unknown)
          : null
      };
    } catch {
      return {
        name,
        data: dataText
      };
    }
  }

  private async readApiError(
    response: Response
  ): Promise<string> {
    try {
      const body =
        (await response.json()) as {
          message?: string;
          errors?: Array<{ message?: string }>;
        };

      return (
        body.errors?.[0]?.message ||
        body.message ||
        'The chat request failed.'
      );
    } catch {
      return 'The assistant could not generate a response.';
    }
  }

  togglePin(id: string): Observable<Conversation> {
    return this.http
      .put<Conversation>(`${this.apiUrl}/conversations/${id}/pin`, {}, { headers: this.getHeaders() })
      .pipe(timeout(this.requestTimeoutMs));
  }

  renameConversation(id: string, newTitle: string): Observable<Conversation> {
    return this.http
      .put<Conversation>(`${this.apiUrl}/conversations/${id}/rename`, { newTitle }, { headers: this.getHeaders() })
      .pipe(timeout(this.requestTimeoutMs));
  }

  // --- Folder endpoints ---

  getFolders(): Observable<ChatFolder[]> {
    return this.http
      .get<ChatFolder[]>(`${this.apiUrl}/folders`, { headers: this.getHeaders() })
      .pipe(timeout(this.requestTimeoutMs));
  }

  createFolder(name: string): Observable<ChatFolder> {
    return this.http
      .post<ChatFolder>(`${this.apiUrl}/folders`, { name }, { headers: this.getHeaders() })
      .pipe(timeout(this.requestTimeoutMs));
  }

  renameFolder(id: string, newName: string): Observable<ChatFolder> {
    return this.http
      .put<ChatFolder>(`${this.apiUrl}/folders/${id}/rename`, { newName }, { headers: this.getHeaders() })
      .pipe(timeout(this.requestTimeoutMs));
  }

  deleteFolder(id: string): Observable<{ success: boolean }> {
    return this.http
      .delete<{ success: boolean }>(`${this.apiUrl}/folders/${id}`, { headers: this.getHeaders() })
      .pipe(timeout(this.requestTimeoutMs));
  }

  moveConversationToFolder(conversationId: string, folderId: string | null): Observable<{ success: boolean }> {
    return this.http
      .put<{ success: boolean }>(`${this.apiUrl}/conversations/${conversationId}/folder`, { folderId }, { headers: this.getHeaders() })
      .pipe(timeout(this.requestTimeoutMs));
  }
}
