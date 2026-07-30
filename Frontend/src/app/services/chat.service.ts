import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { timeout } from 'rxjs/operators';

export interface ChatMessage {
  id?: string;
  role: string;
  content: string;
}

export interface Conversation {
  id: string;
  title: string;
  createdAt: string;
  messages: ChatMessage[];
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

  sendMessage(conversationId: string, message: string): Observable<Conversation> {
    return this.http
      .post<Conversation>(`${this.apiUrl}/send`, { conversationId, message }, { headers: this.getHeaders() })
      .pipe(timeout(this.responseTimeoutMs));
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
}
