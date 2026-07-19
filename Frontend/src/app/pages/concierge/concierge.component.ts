import {
  AfterViewChecked,
  Component,
  ElementRef,
  ViewChild
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink, RouterLinkActive } from '@angular/router';

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
export class ConciergeComponent implements AfterViewChecked {
  @ViewChild('chatMessages') private chatMessagesRef?: ElementRef<HTMLDivElement>;

  inputValue = '';
  isTyping = false;

  // Extra messages appended at runtime (the first three exchanges are the
  // original static conversation and stay directly in the template, unchanged).
  messages: ChatMessage[] = [];

  private shouldScroll = false;

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

  handleSend(): void {
    const text = this.inputValue.trim();
    if (!text) {
      return;
    }

    this.messages.push({ text, isUser: true, time: this.nowLabel() });
    this.inputValue = '';
    this.shouldScroll = true;

    this.isTyping = true;
    this.shouldScroll = true;

    setTimeout(() => {
      this.isTyping = false;
      this.messages.push({
        text: "That sounds like a wonderful plan. I've updated your preferences. Would you like me to check the availability for those dates?",
        isUser: false,
        time: this.nowLabel()
      });
      this.shouldScroll = true;
    }, 1500);
  }
}
