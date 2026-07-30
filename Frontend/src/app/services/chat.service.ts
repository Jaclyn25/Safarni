import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface ChatMessage {
  role: string;
  content: string;
}

export interface Conversation {
  id: string;
  title: string;
  createdAt: string;
  messages: ChatMessage[];
}

export interface ChatStreamHandlers {
  onToken: (token: string) => void;
  onCompleted: (conversation: Conversation) => void;
}

interface ChatStreamEvent {
  type: 'token' | 'completed' | 'error';
  token: string | null;
  conversation: Conversation | null;
}

@Injectable({
  providedIn: 'root'
})
export class ChatService {
  private apiUrl = 'http://localhost:5110/api/Chat';

  constructor(private http: HttpClient) {}

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
    return this.http.post<Conversation>(`${this.apiUrl}/conversations`, { title }, { headers: this.getHeaders() });
  }

  getConversations(): Observable<Conversation[]> {
    return this.http.get<Conversation[]>(`${this.apiUrl}/conversations`, { headers: this.getHeaders() });
  }

  getConversationById(id: string): Observable<Conversation> {
    return this.http.get<Conversation>(`${this.apiUrl}/conversations/${id}`, { headers: this.getHeaders() });
  }

  sendMessage(conversationId: string, message: string): Observable<Conversation> {
    return this.http.post<Conversation>(`${this.apiUrl}/send`, { conversationId, message }, { headers: this.getHeaders() });
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
      throw new Error(await this.readApiError(response));
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
    return this.http.post<Conversation>(`${this.apiUrl}/regenerate`, { conversationId }, { headers: this.getHeaders() });
  }

  deleteConversation(id: string): Observable<{ success: boolean }> {
    return this.http.delete<{ success: boolean }>(`${this.apiUrl}/conversations/${id}`, { headers: this.getHeaders() });
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

    if (
      event.name === 'completed' &&
      data?.conversation
    ) {
      handlers.onCompleted(data.conversation);
      return true;
    }

    if (event.name === 'error') {
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
}
