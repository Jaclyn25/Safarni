import {
  AfterViewChecked,
  Component,
  ElementRef,
  OnInit,
  ViewChild
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { ChatService } from '../../services/chat.service';
import { Subscription } from 'rxjs';

interface ChatMessage {
  text: string;
  isUser: boolean;
  time: string;
}

@Component({
  selector: 'app-concierge',
  standalone: true,
  imports: [FormsModule, RouterLink, RouterLinkActive],
  templateUrl: './concierge.component.html'
})
export class ConciergeComponent implements OnInit, AfterViewChecked {
  @ViewChild('chatMessages') private chatMessagesRef?: ElementRef<HTMLDivElement>;

  inputValue = '';
  isTyping = false;
  conversationId: string | null = null;
  messages: ChatMessage[] = [];
  conversationsList: any[] = [];
  
  private messageSubscription?: Subscription;
  private shouldScroll = false;

  constructor(private chatService: ChatService) {}

  ngOnInit(): void {
    this.loadOrCreateConversation();
  }

  ngAfterViewChecked(): void {
    if (this.shouldScroll && this.chatMessagesRef) {
      const el = this.chatMessagesRef.nativeElement;
      el.scrollTop = el.scrollHeight;
      this.shouldScroll = false;
    }
  }

  private nowLabel(): string {
    return new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  }

  loadOrCreateConversation(): void {
    this.chatService.getConversations().subscribe({
      next: (conversations) => {
        this.conversationsList = conversations || [];
        
        // Check localStorage first
        const savedId = localStorage.getItem('active_conversation_id');
        if (savedId && this.conversationsList.some(c => c.id === savedId)) {
          this.conversationId = savedId;
          this.loadConversationMessages(savedId);
        } else if (this.conversationsList.length > 0) {
          // Load the latest conversation
          const latest = this.conversationsList[this.conversationsList.length - 1];
          this.conversationId = latest.id;
          localStorage.setItem('active_conversation_id', latest.id);
          this.loadConversationMessages(latest.id);
        } else {
          // Create a new one
          this.startNewConversation();
        }
      },
      error: (err) => {
        console.error('Error fetching conversations, starting new one', err);
        this.startNewConversation();
      }
    });
  }

  startNewConversation(): void {
    this.stopResponse();
    this.chatService.startConversation('محادثة جديدة').subscribe({
      next: (conv) => {
        this.conversationId = conv.id;
        localStorage.setItem('active_conversation_id', conv.id);
        
        // Refresh conversations list
        this.chatService.getConversations().subscribe(list => {
          this.conversationsList = list || [];
        });

        this.messages = conv.messages.map(m => ({
          text: m.content,
          isUser: m.role === 'user',
          time: this.nowLabel()
        }));
        this.shouldScroll = true;
      },
      error: (err) => {
        console.error('Failed to start conversation', err);
      }
    });
  }

  loadConversationMessages(id: string): void {
    this.chatService.getConversationById(id).subscribe({
      next: (conv) => {
        this.messages = conv.messages.map(m => ({
          text: m.content,
          isUser: m.role === 'user',
          time: this.nowLabel()
        }));
        this.shouldScroll = true;
      },
      error: (err) => {
        console.error('Failed to load conversation messages', err);
      }
    });
  }

  selectConversation(id: string): void {
    if (this.conversationId === id) return;
    this.stopResponse();
    this.conversationId = id;
    localStorage.setItem('active_conversation_id', id);
    this.loadConversationMessages(id);
  }

  deleteConversation(id: string, event: Event): void {
    event.stopPropagation(); // Prevent selection trigger
    
    if (confirm('هل أنت متأكد من رغبتك في حذف هذه المحادثة؟')) {
      this.chatService.deleteConversation(id).subscribe({
        next: () => {
          this.conversationsList = this.conversationsList.filter(c => c.id !== id);
          
          if (this.conversationId === id) {
            localStorage.removeItem('active_conversation_id');
            this.conversationId = null;
            this.messages = [];
            
            if (this.conversationsList.length > 0) {
              const latest = this.conversationsList[this.conversationsList.length - 1];
              this.selectConversation(latest.id);
            } else {
              this.startNewConversation();
            }
          }
        },
        error: (err) => {
          console.error('Failed to delete conversation', err);
        }
      });
    }
  }

  handleSend(): void {
    const text = this.inputValue.trim();
    if (!text || !this.conversationId) {
      return;
    }

    // Add user message to UI immediately
    this.messages.push({ text, isUser: true, time: this.nowLabel() });
    this.inputValue = '';
    this.shouldScroll = true;
    this.isTyping = true;

    this.messageSubscription = this.chatService.sendMessage(this.conversationId, text).subscribe({
      next: (conv) => {
        this.isTyping = false;
        this.messageSubscription = undefined;
        
        this.messages = conv.messages.map(m => ({
          text: m.content,
          isUser: m.role === 'user',
          time: this.nowLabel()
        }));

        // Refresh list to update titles if changed from "محادثة جديدة"
        const currentConv = this.conversationsList.find(c => c.id === this.conversationId);
        if (currentConv && currentConv.title === 'محادثة جديدة') {
          this.chatService.getConversations().subscribe(list => {
            this.conversationsList = list || [];
          });
        }
        
        this.shouldScroll = true;
      },
      error: (err) => {
        this.isTyping = false;
        this.messageSubscription = undefined;
        this.messages.push({
          text: 'عذراً، حدث خطأ أثناء إرسال الرسالة. يرجى المحاولة مرة أخرى.',
          isUser: false,
          time: this.nowLabel()
        });
        this.shouldScroll = true;
        console.error('Error sending message', err);
      }
    });
  }

  stopResponse(): void {
    if (this.messageSubscription) {
      this.messageSubscription.unsubscribe();
      this.messageSubscription = undefined;
    }
    this.isTyping = false;
  }

  handleRefresh(): void {
    this.startNewConversation();
  }
}
