import {
  AfterViewChecked,
  ChangeDetectorRef,
  Component,
  ElementRef,
  OnDestroy,
  OnInit,
  ViewChild
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  RouterLink,
  RouterLinkActive
} from '@angular/router';
import { Subscription } from 'rxjs';

import {
  ChatService,
  Conversation
} from '../../services/chat.service';

interface ChatMessage {
  text: string;
  isUser: boolean;
  time: string;
  formatted: FormattedMessage;
}

interface FormattedMessage {
  direction: 'rtl' | 'ltr';
  paragraphs: string[];
  links: MessageLink[];
  recommendations: RecommendationItem[];
}

interface MessageLink {
  label: string;
  url: string;
  icon: string;
}

interface RecommendationItem {
  title: string;
  rating?: string;
  description: string;
  link?: MessageLink;
}

@Component({
  selector: 'app-concierge',
  standalone: true,
  imports: [
    FormsModule,
    RouterLink,
    RouterLinkActive
  ],
  templateUrl: './concierge.component.html'
})
export class ConciergeComponent
  implements OnInit, OnDestroy, AfterViewChecked
{
  @ViewChild('chatMessages')
  private chatMessagesRef?: ElementRef<HTMLDivElement>;

  inputValue = '';
  isTyping = false;
  hasStreamingContent = false;

  conversationId: string | null = null;
  messages: ChatMessage[] = [];
  conversationsList: Conversation[] = [];

  private conversationCreationSubscription?: Subscription;
  private abortController: AbortController | null = null;
  private streamingMessageIndex: number | null = null;
  private activeRequestId = 0;
  private shouldScroll = false;

  private readonly urlPattern =
    /https?:\/\/[^\s)\]]+/gi;

  constructor(
    private readonly chatService: ChatService,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadOrCreateConversation();
  }

  ngOnDestroy(): void {
    this.stopResponse();
  }

  ngAfterViewChecked(): void {
    if (
      !this.shouldScroll ||
      !this.chatMessagesRef
    ) {
      return;
    }

    const element =
      this.chatMessagesRef.nativeElement;

    element.scrollTop = element.scrollHeight;
    this.shouldScroll = false;
  }

  loadOrCreateConversation(): void {
    this.chatService.getConversations().subscribe({
      next: (conversations) => {
        this.conversationsList =
          conversations || [];
        this.cdr.markForCheck();

        // Open a fresh local chat while keeping history in the sidebar.
        this.startNewConversation();
      },
      error: (error) => {
        console.error(
          'Error fetching conversations',
          error
        );
        this.startNewConversation();
      }
    });
  }

  startNewConversation(): void {
    this.stopResponse();

    this.conversationId = null;
    this.messages = [];
    this.shouldScroll = true;

    localStorage.removeItem(
      'active_conversation_id'
    );
  }

  loadConversationMessages(id: string): void {
    this.chatService
      .getConversationById(id)
      .subscribe({
        next: (conversation) => {
          this.messages =
            conversation.messages.map(
              (message) =>
                this.toChatMessage(
                  message.content,
                  message.role === 'user'
                )
            );

          this.shouldScroll = true;
        },
        error: (error) => {
          console.error(
            'Failed to load conversation messages',
            error
          );
        }
      });
  }

  selectConversation(id: string): void {
    if (this.conversationId === id) {
      return;
    }

    this.stopResponse();
    this.conversationId = id;

    localStorage.setItem(
      'active_conversation_id',
      id
    );

    this.loadConversationMessages(id);
  }

  deleteConversation(
    id: string,
    event: Event
  ): void {
    event.stopPropagation();

    const confirmed = window.confirm(
      'Are you sure you want to delete this conversation?'
    );

    if (!confirmed) {
      return;
    }

    this.chatService
      .deleteConversation(id)
      .subscribe({
        next: () => {
          this.conversationsList =
            this.conversationsList.filter(
              (conversation) =>
                conversation.id !== id
            );

          if (this.conversationId === id) {
            this.startNewConversation();
          }
        },
        error: (error) => {
          console.error(
            'Failed to delete conversation',
            error
          );
        }
      });
  }

  handleSend(): void {
    const text = this.inputValue.trim();

    if (!text || this.isTyping) {
      return;
    }

    const requestId = ++this.activeRequestId;

    this.messages.push(
      this.toChatMessage(text, true)
    );

    this.inputValue = '';
    this.shouldScroll = true;
    this.isTyping = true;
    this.hasStreamingContent = false;
    this.streamingMessageIndex = null;

    if (this.conversationId) {
      void this.streamMessage(
        this.conversationId,
        text,
        requestId
      );

      return;
    }

    const title = text.length > 30
      ? `${text.slice(0, 27)}...`
      : text;

    this.conversationCreationSubscription =
      this.chatService
        .startConversation(title)
        .subscribe({
          next: (conversation) => {
            this.conversationCreationSubscription =
              undefined;

            if (
              requestId !== this.activeRequestId
            ) {
              return;
            }

            this.conversationId =
              conversation.id;

            localStorage.setItem(
              'active_conversation_id',
              conversation.id
            );

            void this.streamMessage(
              conversation.id,
              text,
              requestId
            );
          },
          error: (error) => {
            this.conversationCreationSubscription =
              undefined;

            if (
              requestId !== this.activeRequestId
            ) {
              return;
            }

            this.isTyping = false;
            this.messages.push(
              this.toChatMessage(
                'Sorry, the conversation could not be created. Please try again.',
                false
              )
            );

            console.error(
              'Error starting conversation',
              error
            );
          }
        });
  }

  stopResponse(): void {
    this.activeRequestId++;

    this.conversationCreationSubscription
      ?.unsubscribe();
    this.conversationCreationSubscription =
      undefined;

    this.abortController?.abort();
    this.abortController = null;

    this.isTyping = false;
    this.hasStreamingContent = false;
    this.streamingMessageIndex = null;
  }

  handleRefresh(): void {
    this.startNewConversation();
  }

  private async streamMessage(
    conversationId: string,
    text: string,
    requestId: number
  ): Promise<void> {
    const controller = new AbortController();
    this.abortController = controller;

    try {
      await this.chatService.streamMessage(
        conversationId,
        text,
        {
          onToken: (token) => {
            if (
              requestId !== this.activeRequestId
            ) {
              return;
            }

            this.appendStreamingToken(token);
          },
          onCompleted: (conversation) => {
            if (
              requestId !== this.activeRequestId
            ) {
              return;
            }

            this.messages =
              conversation.messages.map(
                (message) =>
                  this.toChatMessage(
                    message.content,
                    message.role === 'user'
                  )
              );

            this.streamingMessageIndex = null;
            this.hasStreamingContent = false;
            this.refreshConversationList();
            this.shouldScroll = true;
          }
        },
        controller.signal
      );
    } catch (error: unknown) {
      if (
        requestId !== this.activeRequestId ||
        controller.signal.aborted ||
        (error instanceof DOMException &&
          error.name === 'AbortError')
      ) {
        return;
      }

      const message =
        error instanceof Error
          ? error.message
          : 'The assistant could not complete the response. Please try again.';

      this.messages.push(
        this.toChatMessage(message, false)
      );
      this.shouldScroll = true;

      console.error(
        'Error streaming chat response',
        error
      );
    } finally {
      if (
        requestId === this.activeRequestId
      ) {
        this.abortController = null;
        this.isTyping = false;
        this.hasStreamingContent = false;
        this.streamingMessageIndex = null;
      }
    }
  }

  private appendStreamingToken(token: string): void {
    if (this.streamingMessageIndex === null) {
      const message =
        this.toChatMessage(token, false);

      message.formatted =
        this.formatStreamingMessage(token);

      this.messages.push(message);
      this.streamingMessageIndex =
        this.messages.length - 1;
      this.hasStreamingContent = true;
    } else {
      const message =
        this.messages[
          this.streamingMessageIndex
        ];

      message.text += token;
      message.formatted =
        this.formatStreamingMessage(
          message.text
        );
    }

    this.shouldScroll = true;
    this.cdr.markForCheck();
  }

  private refreshConversationList(): void {
    this.chatService
      .getConversations()
      .subscribe({
        next: (conversations) => {
          this.conversationsList =
            conversations || [];
          this.cdr.markForCheck();
        },
        error: (error) => {
          console.error(
            'Failed to refresh conversations',
            error
          );
        }
      });
  }

  private nowLabel(): string {
    return new Date().toLocaleTimeString(
      [],
      {
        hour: '2-digit',
        minute: '2-digit'
      }
    );
  }

  private toChatMessage(
    text: string,
    isUser: boolean
  ): ChatMessage {
    return {
      text,
      isUser,
      time: this.nowLabel(),
      formatted: this.formatMessage(text)
    };
  }

  private formatStreamingMessage(
    text: string
  ): FormattedMessage {
    return this.formatMessage(text);
  }

  private formatMessage(
    text: string
  ): FormattedMessage {
    const links = this.extractLinks(text);
    const withoutUrls = text
      .replace(this.urlPattern, '')
      .trim();

    const recommendations =
      this.extractRecommendations(
        withoutUrls,
        links
      );

    const cleaned =
      this.cleanMarkdown(withoutUrls);

    return {
      direction:
        this.detectDirection(cleaned),
      paragraphs:
        this.extractParagraphs(
          cleaned,
          recommendations
        ),
      links: links.filter(
        (link) =>
          !recommendations.some(
            (item) =>
              item.link?.url === link.url
          )
      ),
      recommendations
    };
  }

  private detectDirection(
    text: string
  ): 'rtl' | 'ltr' {
    return /[\u0600-\u06FF]/.test(text)
      ? 'rtl'
      : 'ltr';
  }

  private cleanMarkdown(text: string): string {
    return text
      .replace(/\*\*(.*?)\*\*/g, '$1')
      .replace(/__(.*?)__/g, '$1')
      .replace(/\[(.*?)\]\((.*?)\)/g, '$1')
      .replace(/^\s*[-*]\s+/gm, '')
      .replace(/[ \t]+/g, ' ')
      .replace(/\n{3,}/g, '\n\n')
      .trim();
  }

  private extractLinks(text: string): MessageLink[] {
    const matches =
      text.match(this.urlPattern) || [];

    return matches.map((rawUrl, index) => {
      const url =
        rawUrl.replace(/[.,،؛]+$/, '');

      const host = this.getHostname(url);
      const isMap =
        /maps\.google|google\.com\/maps|place_id/i
          .test(url);

      return {
        url,
        icon: isMap
          ? 'map'
          : 'open_in_new',
        label: isMap
          ? 'View on map'
          : host || `Open link ${index + 1}`
      };
    });
  }

  private getHostname(url: string): string {
    try {
      return new URL(url)
        .hostname
        .replace(/^www\./, '');
    } catch {
      return '';
    }
  }

  private extractRecommendations(
    text: string,
    links: MessageLink[]
  ): RecommendationItem[] {
    const normalized = this.cleanMarkdown(text)
      .replace(/\s*\n+\s*/g, ' ');

    const candidatePattern =
      /(?:^|\s)(?:\d+[.)]\s*)?([^:：.!؟]{3,80})\s*[:：]\s*([^:：]+?)(?=(?:\s+\d+[.)]\s*)?[^:：.!؟]{3,80}\s*[:：]|$)/g;

    const items: RecommendationItem[] = [];
    let match: RegExpExecArray | null;

    while (
      (match =
        candidatePattern.exec(normalized)) !==
      null
    ) {
      const item = this.parseRecommendation(
        match[1],
        match[2],
        links[items.length]
      );

      if (item) {
        items.push(item);
      }
    }

    return items.length >= 2
      ? items.slice(0, 6)
      : [];
  }

  private parseRecommendation(
    titleText: string,
    bodyText: string,
    link?: MessageLink
  ): RecommendationItem | null {
    const title = titleText
      .replace(/^[*-]\s*/, '')
      .trim();

    const body = bodyText.trim();
    const rating = body
      .match(
        /(?:⭐\s*)?([0-5](?:\.\d)?\s*\/\s*5)/
      )?.[1]
      ?.replace(/\s+/g, '');

    if (
      !title ||
      title.length > 80 ||
      body.length < 12 ||
      !rating
    ) {
      return null;
    }

    return {
      title,
      rating,
      description: this.compactDescription(
        body.replace(
          /(?:⭐\s*)?[0-5](?:\.\d)?\s*\/\s*5/g,
          ''
        )
      ),
      link
    };
  }

  private compactDescription(text: string): string {
    return text
      .replace(/\s*\[.*?\]\s*/g, ' ')
      .replace(/\s+/g, ' ')
      .replace(/^[\s,،:：.-]+/, '')
      .slice(0, 220)
      .trim();
  }

  private extractParagraphs(
    text: string,
    recommendations: RecommendationItem[]
  ): string[] {
    if (recommendations.length) {
      const intro = text
        .split(
          /(?:\d+[.)]\s*)?[^:：.!؟]{3,80}\s*[:：]/
        )[0]
        ?.trim();

      return intro ? [intro] : [];
    }

    return text
      .split(/\n{2,}/)
      .map((paragraph) =>
        paragraph
          .split('\n')
          .map((line) =>
            line.replace(/[ \t]+/g, ' ').trim()
          )
          .join('\n')
          .trim()
      )
      .filter(Boolean)
      .slice(0, 6);
  }
}
