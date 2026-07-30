import {
  AfterViewChecked,
  Component,
  ElementRef,
  OnInit,
  ViewChild,
  ChangeDetectorRef
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { NgTemplateOutlet } from '@angular/common';
import { ChatService } from '../../services/chat.service';
import { Subscription } from 'rxjs';

export interface ChatFolder {
  id: string;
  name: string;
}

interface ChatMessage {
  id?: string;
  text: string;
  isUser: boolean;
  time: string;
  formatted: FormattedMessage;
  reaction?: 'like' | 'dislike';
}

interface FormattedMessage {
  direction: 'rtl' | 'ltr';
  paragraphs: string[];
  links: MessageLink[];
  recommendations: RecommendationItem[];
  suggestions: string[];
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
  imports: [FormsModule, RouterLink, RouterLinkActive, NgTemplateOutlet],
  templateUrl: './concierge.component.html'
})
export class ConciergeComponent implements OnInit, AfterViewChecked {
  @ViewChild('chatMessages') private chatMessagesRef?: ElementRef<HTMLDivElement>;

  inputValue = '';
  isTyping = false;
  conversationId: string | null = null;
  messages: ChatMessage[] = [];
  conversationsList: any[] = [];

  editingMessageId: string | null = null;
  editingMessageContent = '';

  isSidebarOpen = true;
  searchQuery = '';
  pinnedChats = new Set<string>();

  // Renaming State
  customTitles: { [convId: string]: string } = {};
  renamingChatId: string | null = null;
  newChatName = '';

  // Sections State
  isPinnedExpanded = true;
  isRecentExpanded = true;
  isFoldersExpanded = true;

  // Folders State
  folders: ChatFolder[] = [];
  chatFolderMapping: { [convId: string]: string } = {}; // convId -> folderId
  expandedFolders: Set<string> = new Set<string>(); // To toggle folders in sidebar

  // Folder Creation Modal
  showCreateFolderModal = false;
  newFolderName = '';

  private messageSubscription?: Subscription;
  private shouldScroll = false;
  private readonly urlPattern = /https?:\/\/[^\s)\]]+/gi;

  constructor(private chatService: ChatService, private cdr: ChangeDetectorRef) {
    const savedPins = localStorage.getItem('safarni_pinned_chats');
    if (savedPins) {
      try { this.pinnedChats = new Set(JSON.parse(savedPins)); } catch (e) { }
    }

    const savedTitles = localStorage.getItem('safarni_custom_titles');
    if (savedTitles) {
      try { this.customTitles = JSON.parse(savedTitles); } catch (e) { }
    }

    const savedFolders = localStorage.getItem('safarni_folders');
    if (savedFolders) {
      try { this.folders = JSON.parse(savedFolders); } catch (e) { }
    }

    const savedMapping = localStorage.getItem('safarni_chat_folders');
    if (savedMapping) {
      try { this.chatFolderMapping = JSON.parse(savedMapping); } catch (e) { }
    }

    // Expand all folders by default
    this.folders.forEach(f => this.expandedFolders.add(f.id));
  }

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
        this.cdr.detectChanges();

        // Requirement 1: Open an empty chat on page load
        this.startNewConversation();
      },
      error: (err) => {
        console.error('Error fetching conversations', err);
        this.startNewConversation();
      }
    });
  }

  startNewConversation(): void {
    this.stopResponse();

    // Requirement 3: +new chat only opens an empty chat locally
    this.conversationId = null;
    this.messages = [];
    this.shouldScroll = true;
    localStorage.removeItem('active_conversation_id');
  }

  loadConversationMessages(id: string): void {
    this.chatService.getConversationById(id).subscribe({
      next: (conv) => {
        this.messages = conv.messages.map(m => this.toChatMessage(m.content, m.role === 'user', m.id));
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

  // Delete Modal State
  showDeleteModal = false;
  deleteTargetId: string | null = null;
  deleteType: 'chat' | 'folder' | null = null;

  openDeleteChatModal(id: string, event: Event): void {
    event.stopPropagation();
    this.deleteTargetId = id;
    this.deleteType = 'chat';
    this.showDeleteModal = true;
  }

  openDeleteFolderModal(id: string, event: Event): void {
    event.stopPropagation();
    this.deleteTargetId = id;
    this.deleteType = 'folder';
    this.showDeleteModal = true;
  }

  deleteConversation(id: string, event: Event): void {
    event.stopPropagation(); // Prevent selection trigger
    this.openDeleteChatModal(id, event);
  }

  closeDeleteModal(): void {
    this.showDeleteModal = false;
    this.deleteTargetId = null;
    this.deleteType = null;
  }

  confirmDelete(): void {
    if (!this.deleteTargetId || !this.deleteType) return;

    if (this.deleteType === 'chat') {
      const id = this.deleteTargetId;
      this.chatService.deleteConversation(id).subscribe({
        next: () => {
          this.conversationsList = this.conversationsList.filter(c => c.id !== id);
          if (this.conversationId === id) {
            this.startNewConversation();
          }
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error('Failed to delete conversation', err);
        }
      });
    } else if (this.deleteType === 'folder') {
      const folderId = this.deleteTargetId;
      this.folders = this.folders.filter(f => f.id !== folderId);
      // Remove mapping for this folder
      Object.keys(this.chatFolderMapping).forEach(convId => {
        if (this.chatFolderMapping[convId] === folderId) {
          delete this.chatFolderMapping[convId];
        }
      });
      this.saveFoldersState();
      this.cdr.detectChanges();
    }

    this.closeDeleteModal();
  }

  get unpinnedConversations(): any[] {
    let list = this.conversationsList.filter(c => !this.pinnedChats.has(c.id));
    if (this.searchQuery.trim()) {
      const query = this.searchQuery.toLowerCase();
      list = list.filter(c => this.getChatTitle(c).toLowerCase().includes(query));
    }
    return list;
  }

  get pinnedConversations(): any[] {
    let list = this.conversationsList.filter(c => this.pinnedChats.has(c.id));
    if (this.searchQuery.trim()) {
      const query = this.searchQuery.toLowerCase();
      list = list.filter(c => this.getChatTitle(c).toLowerCase().includes(query));
    }
    return list;
  }

  getChatTitle(conv: any): string {
    return this.customTitles[conv.id] || conv.title || 'Chat';
  }

  // Renaming Logic
  startRename(conv: any, event: Event): void {
    event.stopPropagation();
    this.renamingChatId = conv.id;
    this.newChatName = this.getChatTitle(conv);
  }

  saveRename(event: Event): void {
    event.stopPropagation();
    if (this.renamingChatId && this.newChatName.trim()) {
      this.customTitles[this.renamingChatId] = this.newChatName.trim();
      localStorage.setItem('safarni_custom_titles', JSON.stringify(this.customTitles));
    }
    this.renamingChatId = null;
  }

  cancelRename(event: Event): void {
    event.stopPropagation();
    this.renamingChatId = null;
  }

  // Folders Logic
  openCreateFolderModal(): void {
    this.showCreateFolderModal = true;
    this.newFolderName = '';
  }

  closeCreateFolderModal(): void {
    this.showCreateFolderModal = false;
    this.newFolderName = '';
  }

  confirmCreateFolder(): void {
    const name = this.newFolderName.trim();
    if (name) {
      const newFolder: ChatFolder = {
        id: 'folder_' + Date.now().toString(),
        name: name
      };
      this.folders.push(newFolder);
      this.expandedFolders.add(newFolder.id);
      this.saveFoldersState();
      this.closeCreateFolderModal();
    }
  }

  deleteFolder(folderId: string, event: Event): void {
    event.stopPropagation();
    this.openDeleteFolderModal(folderId, event);
  }

  assignToFolder(convId: string, folderId: string | null, event: Event): void {
    event.stopPropagation();
    if (folderId) {
      this.chatFolderMapping[convId] = folderId;
    } else {
      delete this.chatFolderMapping[convId];
    }
    this.saveFoldersState();
  }

  toggleFolderExpand(folderId: string): void {
    if (this.expandedFolders.has(folderId)) {
      this.expandedFolders.delete(folderId);
    } else {
      this.expandedFolders.add(folderId);
    }
  }

  private saveFoldersState(): void {
    localStorage.setItem('safarni_folders', JSON.stringify(this.folders));
    localStorage.setItem('safarni_chat_folders', JSON.stringify(this.chatFolderMapping));
  }

  getFolderChats(folderId: string): any[] {
    return this.unpinnedConversations.filter(c => this.chatFolderMapping[c.id] === folderId);
  }

  get uncategorizedChats(): any[] {
    return this.unpinnedConversations.filter(c => !this.chatFolderMapping[c.id]);
  }

  toggleSidebar(): void {
    this.isSidebarOpen = !this.isSidebarOpen;
  }

  togglePin(id: string, event: Event): void {
    event.stopPropagation();
    if (this.pinnedChats.has(id)) {
      this.pinnedChats.delete(id);
    } else {
      this.pinnedChats.add(id);
    }
    localStorage.setItem('safarni_pinned_chats', JSON.stringify(Array.from(this.pinnedChats)));
  }

  handleReaction(msg: ChatMessage, type: 'like' | 'dislike'): void {
    if (msg.reaction === type) {
      msg.reaction = undefined; // toggle off
    } else {
      msg.reaction = type;
    }
  }

  handleSend(): void {
    const text = this.inputValue.trim();
    if (!text) {
      return;
    }

    // Add user message to UI immediately
    this.messages.push(this.toChatMessage(text, true));
    this.inputValue = '';
    this.shouldScroll = true;
    this.isTyping = true;

    if (!this.conversationId) {
      // Create a new conversation on the backend first
      const title = text.length > 30 ? text.substring(0, 30) + '...' : text;
      this.chatService.startConversation(title).subscribe({
        next: (conv) => {
          this.conversationId = conv.id;
          localStorage.setItem('active_conversation_id', conv.id);

          // Now send the actual message
          this._sendMessage(conv.id, text);
        },
        error: (err) => {
          this.isTyping = false;
          this.messages.push(this.toChatMessage('عذراً، حدث خطأ أثناء إنشاء المحادثة.', false));
          console.error('Error starting conversation', err);
        }
      });
    } else {
      // Existing conversation
      this._sendMessage(this.conversationId, text);
    }
  }

  private _sendMessage(convId: string, text: string): void {
    this.messageSubscription = this.chatService.sendMessage(convId, text).subscribe({
      next: (conv) => {
        this.isTyping = false;
        this.messageSubscription = undefined;

        this.messages = conv.messages.map(m => this.toChatMessage(m.content, m.role === 'user', m.id));
        this.shouldScroll = true;
        this.cdr.detectChanges(); // Update UI immediately

        // Refresh the list to ensure the title is updated or newly created chat is visible
        this.chatService.getConversations().subscribe({
          next: (list) => {
            this.conversationsList = list || [];
            this.cdr.detectChanges();
          },
          error: (err) => console.error('Error fetching conversations', err)
        });
      },
      error: (err) => {
        this.isTyping = false;
        this.messageSubscription = undefined;
        this.messages.push(this.toChatMessage('عذراً، حدث خطأ أثناء إرسال الرسالة. يرجى المحاولة مرة أخرى.', false));
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

  handleRegenerate(): void {
    if (!this.conversationId || this.isTyping) return;
    this.isTyping = true;
    this.shouldScroll = true;
    this.messageSubscription = this.chatService.regenerateResponse(this.conversationId).subscribe({
      next: (conv) => {
        this.isTyping = false;
        this.messageSubscription = undefined;
        this.messages = conv.messages.map(m => this.toChatMessage(m.content, m.role === 'user', m.id));
        this.shouldScroll = true;
        this.cdr.detectChanges();

        // Refresh conversations list to update title if needed
        this.chatService.getConversations().subscribe({
          next: (list) => {
            this.conversationsList = list || [];
            this.cdr.detectChanges();
          }
        });
      },
      error: (err) => {
        this.isTyping = false;
        this.messageSubscription = undefined;
        this.messages.push(this.toChatMessage('عذراً، حدث خطأ أثناء إعادة توليد الرسالة.', false));
        this.shouldScroll = true;
        this.cdr.detectChanges();
        console.error('Error regenerating', err);
      }
    });
  }

  handleEdit(msg: ChatMessage): void {
    if (!msg.id || !this.conversationId || this.isTyping) return;
    this.editingMessageId = msg.id;
    this.editingMessageContent = msg.text;
  }

  cancelEdit(): void {
    this.editingMessageId = null;
    this.editingMessageContent = '';
  }

  saveEdit(): void {
    if (!this.editingMessageId || !this.conversationId || this.isTyping) return;
    const newContent = this.editingMessageContent.trim();
    if (!newContent) {
      this.cancelEdit();
      return;
    }

    const msgId = this.editingMessageId;
    this.cancelEdit();
    this.isTyping = true;

    // Optimistically update UI
    const targetIdx = this.messages.findIndex(m => m.id === msgId);
    if (targetIdx !== -1) {
      this.messages = this.messages.slice(0, targetIdx);
      this.messages.push(this.toChatMessage(newContent, true, msgId));
    }

    this.shouldScroll = true;

    this.messageSubscription = this.chatService.editMessage(this.conversationId, msgId, newContent).subscribe({
      next: (conv) => {
        this.isTyping = false;
        this.messageSubscription = undefined;
        this.messages = conv.messages.map(m => this.toChatMessage(m.content, m.role === 'user', m.id));
        this.shouldScroll = true;
        this.cdr.detectChanges();

        // Refresh conversations list to update title if needed
        this.chatService.getConversations().subscribe({
          next: (list) => {
            this.conversationsList = list || [];
            this.cdr.detectChanges();
          }
        });
      },
      error: (err) => {
        this.isTyping = false;
        this.messageSubscription = undefined;
        this.messages.push(this.toChatMessage('عذراً، حدث خطأ أثناء تعديل الرسالة.', false));
        this.shouldScroll = true;
        this.cdr.detectChanges();
        console.error('Error editing', err);
      }
    });
  }

  handleCopy(text: string): void {
    navigator.clipboard.writeText(text).then(() => {
      // Optional: show a small toast or indicator
    }).catch(err => {
      console.error('Failed to copy text', err);
    });
  }

  sendSuggestion(suggestion: string): void {
    if (this.isTyping) return;
    this.inputValue = suggestion;
    this.handleSend();
  }

  private toChatMessage(text: string, isUser: boolean, id?: string): ChatMessage {
    return {
      id,
      text,
      isUser,
      time: this.nowLabel(),
      formatted: this.formatMessage(text)
    };
  }

  private formatMessage(text: string): FormattedMessage {
    const suggestions: string[] = [];
    let cleanedText = text;

    // Parse [SUGGESTION: ...]
    const suggestionRegex = /\[SUGGESTION:\s*(.*?)\]/gi;
    let match;
    while ((match = suggestionRegex.exec(text)) !== null) {
      if (match[1]) suggestions.push(match[1].trim());
    }
    cleanedText = cleanedText.replace(suggestionRegex, '').trim();

    const links = this.extractLinks(cleanedText);
    const withoutUrls = cleanedText.replace(this.urlPattern, '').trim();
    const recommendations = this.extractRecommendations(withoutUrls, links);
    const cleaned = this.cleanMarkdown(withoutUrls);

    return {
      direction: this.detectDirection(cleaned),
      paragraphs: this.extractParagraphs(cleaned, recommendations),
      links: links.filter(link => !recommendations.some(item => item.link?.url === link.url)),
      recommendations,
      suggestions
    };
  }

  private detectDirection(text: string): 'rtl' | 'ltr' {
    return /[\u0600-\u06FF]/.test(text) ? 'rtl' : 'ltr';
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
    const matches = text.match(this.urlPattern) || [];

    return matches.map((rawUrl, index) => {
      const url = rawUrl.replace(/[.,،؛]+$/, '');
      const host = this.getHostname(url);
      const isMap = /maps\.google|google\.com\/maps|place_id/i.test(url);

      return {
        url,
        icon: isMap ? 'map' : 'open_in_new',
        label: isMap ? 'View on map' : (host || `Open link ${index + 1}`)
      };
    });
  }

  private getHostname(url: string): string {
    try {
      return new URL(url).hostname.replace(/^www\./, '');
    } catch {
      return '';
    }
  }

  private extractRecommendations(text: string, links: MessageLink[]): RecommendationItem[] {
    const normalized = this.cleanMarkdown(text).replace(/\s*\n+\s*/g, ' ');
    const candidatePattern = /(?:^|\s)(?:\d+[.)]\s*)?([^:：.!؟]{3,80})\s*[:：]\s*([^:：]+?)(?=(?:\s+\d+[.)]\s*)?[^:：.!؟]{3,80}\s*[:：]|$)/g;
    const items: RecommendationItem[] = [];
    let match: RegExpExecArray | null;

    while ((match = candidatePattern.exec(normalized)) !== null) {
      const item = this.parseRecommendation(match[1], match[2], links[items.length]);
      if (item) {
        items.push(item);
      }
    }

    return items.length >= 2 ? items.slice(0, 6) : [];
  }

  private parseRecommendation(titleText: string, bodyText: string, link?: MessageLink): RecommendationItem | null {
    const title = titleText.replace(/^[*-]\s*/, '').trim();
    const body = bodyText.trim();
    const rating = body.match(/(?:⭐\s*)?([0-5](?:\.\d)?\s*\/\s*5)/)?.[1]?.replace(/\s+/g, '');

    if (!title || title.length > 80 || body.length < 12 || !rating) {
      return null;
    }

    return {
      title,
      rating,
      description: this.compactDescription(body.replace(/(?:⭐\s*)?[0-5](?:\.\d)?\s*\/\s*5/g, '')),
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

  private extractParagraphs(text: string, recommendations: RecommendationItem[]): string[] {
    if (recommendations.length) {
      const intro = text.split(/(?:\d+[.)]\s*)?[^:：.!؟]{3,80}\s*[:：]/)[0]?.trim();
      return intro ? [intro] : [];
    }

    return text
      .split(/\n{2,}|(?<=[.!؟])\s+(?=[\u0600-\u06FFA-Z])/)
      .map(paragraph => paragraph.replace(/\s+/g, ' ').trim())
      .filter(Boolean)
      .slice(0, 6);
  }
}
