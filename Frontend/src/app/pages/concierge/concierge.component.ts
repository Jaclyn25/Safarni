import {
  AfterViewChecked,
  ChangeDetectorRef,
  Component,
  ElementRef,
  NgZone,
  OnDestroy,
  OnInit,
  ViewChild,
  inject,
  HostListener
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { Subscription } from 'rxjs';
import { ChatService, Conversation, ChatFolder } from '../../services/chat.service';

export interface ItineraryDay {
  dayNumber: number;
  title: string;
  morning?: string;
  afternoon?: string;
  evening?: string;
  budget?: string;
}

export interface ActionChip {
  label: string;
  icon: string;
  promptText: string;
  actionType?: 'planner' | 'prompt';
}

export interface MessageLink {
  label: string;
  url: string;
  icon: string;
}

export interface FormattedMessage {
  direction: 'rtl' | 'ltr';
  summary?: string;
  paragraphs: string[];
  links: MessageLink[];
  itineraryDays: ItineraryDay[];
  actionChips: ActionChip[];
}

export interface ChatMessage {
  id: string;
  text: string;
  isUser: boolean;
  time: string;
  kind: 'normal' | 'error';
  formatted: FormattedMessage;
  versions?: string[];
  activeVersionIndex?: number;
  isEditing?: boolean;
  editText?: string;
  copied?: boolean;
  isRegenerating?: boolean;
  action?: 'new-chat';
  feedback?: 'like' | 'dislike' | null;
  isStreaming?: boolean;
  streamedText?: string;
  isBookmarked?: boolean;
}

export interface GroupedConversations {
  pinned: Conversation[];
  chats: Conversation[];
}

@Component({
  selector: 'app-concierge',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, RouterLinkActive],
  templateUrl: './concierge.component.html',
  styleUrls: ['./concierge.component.css']
})
export class ConciergeComponent implements OnInit, OnDestroy, AfterViewChecked {
  @ViewChild('chatMessages') private chatMessagesRef?: ElementRef<HTMLDivElement>;
  @ViewChild('messageTextarea') private textareaRef?: ElementRef<HTMLTextAreaElement>;

  private chatService = inject(ChatService);
  private cdr = inject(ChangeDetectorRef);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private zone = inject(NgZone);

  inputValue = '';
  searchConvQuery = '';
  isTyping = false;
  isLoadingConversation = false;
  isSidebarOpen = true;
  conversationId: string | null = null;
  messages: ChatMessage[] = [];
  conversationsList: Conversation[] = [];
  folders: ChatFolder[] = [];
  readonly maxMessageLength = 4000;

  // Empty State Prompt Cards
  emptyStateCards = [
    {
      title: 'Build a 5-day trip',
      subtitle: 'Day-by-day itinerary with budget & spots',
      prompt: 'Build a complete 5-day itinerary for a relaxed vacation with estimated budget',
      icon: 'map'
    },
    {
      title: 'Compare two destinations',
      subtitle: 'Maldives vs Bali vs Thailand',
      prompt: 'Compare Maldives vs Thailand for a 7-day beach vacation including cost and vibe',
      icon: 'compare_arrows'
    },
    {
      title: 'Visa & travel requirements',
      subtitle: 'Passport, visa info & tips',
      prompt: 'What are the visa requirements and best time to visit for tourists?',
      icon: 'verified_user'
    },
    {
      title: 'Find hidden gems',
      subtitle: 'Secret spots & local dining',
      prompt: 'Suggest hidden gems and unique local experiences away from crowded tourist spots',
      icon: 'diamond'
    }
  ];

  private messageSubscription?: Subscription;
  private abortController: AbortController | null = null;
  private streamingMessageIndex: number | null = null;
  private activeRequestId = 0;
  private shouldScroll = false;
  private readonly urlPattern = /https?:\/\/[^\s)\]]+/gi;
  private typewriterInterval: any;
  
  // Bookmarks State
  bookmarks: ChatMessage[] = [];
  isBookmarksModalOpen = false;
  
  get isGenerating(): boolean {
    return this.isTyping || this.messages.some(m => m.isStreaming);
  }

  constructor() {}

  ngOnInit(): void {
    this.loadBookmarks();
    this.loadOrCreateConversation();

    // Handle incoming query param ?q=... from Explore or Destination pages
    this.route.queryParams.subscribe(params => {
      const q = params['q'];
      if (q && q.trim()) {
        setTimeout(() => {
          this.inputValue = q.trim();
          this.handleSend();
        }, 400);
      }
    });
  }

  ngOnDestroy(): void {
    this.stopResponse();
  }

  ngAfterViewChecked(): void {
    if (this.shouldScroll && this.chatMessagesRef) {
      const el = this.chatMessagesRef.nativeElement;
      el.scrollTop = el.scrollHeight;
      this.shouldScroll = false;
    }
  }

  loadOrCreateConversation(): void {
    // Load both folders and conversations concurrently
    this.chatService.getFolders().subscribe({
      next: (folders) => {
        this.folders = folders || [];
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error fetching folders', err);
      }
    });

    this.chatService.getConversations().subscribe({
      next: (conversations) => {
        this.conversationsList = conversations || [];
        this.cdr.detectChanges();
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
    this.isLoadingConversation = false;
    this.conversationId = null;
    this.messages = [];
    this.shouldScroll = true;
    localStorage.removeItem('active_conversation_id');
  }

  loadConversationMessages(id: string): void {
    this.isLoadingConversation = true;
    this.messages = [];

    this.chatService.getConversationById(id).subscribe({
      next: (conv) => {
        this.isLoadingConversation = false;
        this.clearTypewriter();
        this.messages = conv.messages.map(m => this.toChatMessage(m.content, m.role === 'user', 'normal', m.id));
        this.shouldScroll = true;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isLoadingConversation = false;
        console.error('Failed to load conversation messages', err);
        this.addErrorMessage('Unable to load this conversation. Please try again.');
        this.cdr.detectChanges();
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

  openConversation(id: string): void {
    if (id) {
      this.selectConversation(id);
    } else {
      this.startNewConversation();
    }
  }

  // --- Custom Delete Confirmation Modal State & Handlers ---
  showDeleteModal = false;
  deletingConvId: string | null = null;
  deletingConvTitle: string = '';
  isDeletingConversation = false;

  confirmDeleteConversation(id: string, title: string, event: Event): void {
    event.stopPropagation();
    this.deletingConvId = id;
    this.deletingConvTitle = title || 'this trip chat';
    this.showDeleteModal = true;
  }

  cancelDeleteConversation(): void {
    this.showDeleteModal = false;
    this.deletingConvId = null;
    this.deletingConvTitle = '';
  }

  executeDeleteConversation(): void {
    if (!this.deletingConvId || this.isDeletingConversation) return;

    const id = this.deletingConvId;
    this.isDeletingConversation = true;

    this.chatService.deleteConversation(id).subscribe({
      next: () => {
        this.isDeletingConversation = false;
        this.showDeleteModal = false;
        this.conversationsList = this.conversationsList.filter(c => c.id !== id);

        if (this.conversationId === id) {
          this.startNewConversation();
        }

        this.deletingConvId = null;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isDeletingConversation = false;
        this.showDeleteModal = false;
        console.error('Failed to delete conversation', err);
        this.addErrorMessage('Could not delete this conversation. Please try again.');
        this.deletingConvId = null;
        this.cdr.detectChanges();
      }
    });
  }

  // --- Custom Context Menu State & Handlers ---
  activeMenuConvId: string | null = null;

  @HostListener('document:click')
  onDocumentClick() {
    if (this.activeMenuConvId) {
      this.closeConvMenu();
    }
  }

  toggleConvMenu(id: string, event: Event): void {
    event.stopPropagation();
    this.activeMenuConvId = this.activeMenuConvId === id ? null : id;
  }

  closeConvMenu(): void {
    this.activeMenuConvId = null;
  }

  // --- Custom Rename Confirmation Modal State & Handlers ---
  showRenameModal = false;
  renamingConvId: string | null = null;
  renamingConvOldTitle: string = '';
  newTitleInput: string = '';
  isRenamingConversation = false;

  openRenameModal(id: string, title: string, event: Event): void {
    event.stopPropagation();
    this.renamingConvId = id;
    this.renamingConvOldTitle = title;
    this.newTitleInput = title;
  }

  cancelRenameConversation(): void {
    this.renamingConvId = null;
    this.renamingConvOldTitle = '';
    this.newTitleInput = '';
  }

  executeRenameConversation(): void {
    if (!this.renamingConvId || this.isRenamingConversation) return;

    const newTitle = this.newTitleInput.trim();
    if (!newTitle || newTitle === this.renamingConvOldTitle) {
      this.cancelRenameConversation();
      return;
    }

    const id = this.renamingConvId;
    this.isRenamingConversation = true;

    this.chatService.renameConversation(id, newTitle).subscribe({
      next: (updatedConv) => {
        this.isRenamingConversation = false;
        this.renamingConvId = null;

        const idx = this.conversationsList.findIndex(c => c.id === id);
        if (idx !== -1) {
          this.conversationsList[idx] = { ...this.conversationsList[idx], title: newTitle };
          this.conversationsList = [...this.conversationsList];
        }

        for (let i = 0; i < this.folders.length; i++) {
          if (this.folders[i].conversations) {
            const fIdx = this.folders[i].conversations.findIndex(c => c.id === id);
            if (fIdx !== -1) {
              this.folders[i].conversations[fIdx] = { ...this.folders[i].conversations[fIdx], title: newTitle };
              this.folders[i] = { ...this.folders[i] };
            }
          }
        }
        this.folders = [...this.folders];

        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error renaming conversation', err);
        this.isRenamingConversation = false;
      }
    });
  }

  togglePin(id: string, event: Event): void {
    event.stopPropagation();

    const conv = this.conversationsList.find(c => c.id === id);
    if (conv) {
      conv.isPinned = !conv.isPinned;
    }

    this.chatService.togglePin(id).subscribe({
      next: (updatedConv) => {
        const index = this.conversationsList.findIndex(c => c.id === id);
        if (index !== -1) {
          this.conversationsList[index] = updatedConv;
          this.cdr.detectChanges();
        }
      },
      error: (err) => {
        console.error('Failed to toggle pin', err);
        if (conv) conv.isPinned = !conv.isPinned;
        this.cdr.detectChanges();
      }
    });
  }

  get groupedConversations(): GroupedConversations {
    const query = this.searchConvQuery.trim().toLowerCase();
    const filtered = this.conversationsList.filter(c => !query || c.title.toLowerCase().includes(query));

    const pinned: Conversation[] = [];
    const chats: Conversation[] = [];

    filtered.forEach(conv => {
      if (conv.folderId) return; // Skip folder conversations in groups

      if (conv.isPinned) {
        pinned.push(conv);
      } else {
        chats.push(conv);
      }
    });

    return { pinned, chats };
  }

  // --- Folder Logic ---
  showCreateFolderModal = false;
  newFolderNameInput = '';
  isCreatingFolder = false;

  openCreateFolderModal(): void {
    this.newFolderNameInput = '';
    this.showCreateFolderModal = true;
  }

  cancelCreateFolder(): void {
    this.showCreateFolderModal = false;
    this.newFolderNameInput = '';
  }

  executeCreateFolder(): void {
    const name = this.newFolderNameInput.trim();
    if (!name || this.isCreatingFolder) return;

    this.isCreatingFolder = true;
    this.chatService.createFolder(name).subscribe({
      next: (folder) => {
        this.isCreatingFolder = false;
        this.showCreateFolderModal = false;
        if (!folder.conversations) folder.conversations = [];
        this.folders.unshift(folder);
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isCreatingFolder = false;
        console.error('Error creating folder', err);
        this.cdr.detectChanges();
      }
    });
  }

  showRenameFolderModal = false;
  renamingFolderId: string | null = null;
  renamingFolderOldName = '';
  newFolderNameEdit = '';
  isRenamingFolder = false;

  openRenameFolderModal(id: string, name: string, event: Event): void {
    event.stopPropagation();
    this.renamingFolderId = id;
    this.renamingFolderOldName = name;
    this.newFolderNameEdit = name;
  }

  cancelRenameFolder(): void {
    this.renamingFolderId = null;
    this.renamingFolderOldName = '';
    this.newFolderNameEdit = '';
  }

  executeRenameFolder(): void {
    if (!this.renamingFolderId || this.isRenamingFolder) return;
    const newName = this.newFolderNameEdit.trim();
    if (!newName || newName === this.renamingFolderOldName) {
      this.cancelRenameFolder();
      return;
    }

    const id = this.renamingFolderId;
    this.isRenamingFolder = true;
    this.chatService.renameFolder(id, newName).subscribe({
      next: (updatedFolder) => {
        this.isRenamingFolder = false;
        this.renamingFolderId = null;
        const idx = this.folders.findIndex(f => f.id === id);
        if (idx !== -1) {
          this.folders[idx].name = updatedFolder.name;
          this.cdr.detectChanges();
        }
      },
      error: (err) => {
        this.isRenamingFolder = false;
        console.error('Error renaming folder', err);
        this.cdr.detectChanges();
      }
    });
  }

  showDeleteFolderModal = false;
  deletingFolderId: string | null = null;
  deletingFolderName = '';
  isDeletingFolder = false;

  confirmDeleteFolder(id: string, name: string, event: Event): void {
    event.stopPropagation();
    this.deletingFolderId = id;
    this.deletingFolderName = name;
    this.showDeleteFolderModal = true;
  }

  cancelDeleteFolder(): void {
    this.showDeleteFolderModal = false;
    this.deletingFolderId = null;
    this.deletingFolderName = '';
  }

  executeDeleteFolder(): void {
    if (!this.deletingFolderId || this.isDeletingFolder) return;

    const id = this.deletingFolderId;
    this.isDeletingFolder = true;

    this.chatService.deleteFolder(id).subscribe({
      next: () => {
        this.isDeletingFolder = false;
        this.showDeleteFolderModal = false;

        // Remove folder
        const folderIdx = this.folders.findIndex(f => f.id === id);
        if (folderIdx !== -1) {
          // move conversations back to root
          const folderConvs = this.folders[folderIdx].conversations || [];
          folderConvs.forEach(fc => {
            const listConv = this.conversationsList.find(c => c.id === fc.id);
            if (listConv) listConv.folderId = null;
          });
          this.folders.splice(folderIdx, 1);
        }

        this.deletingFolderId = null;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isDeletingFolder = false;
        this.showDeleteFolderModal = false;
        console.error('Failed to delete folder', err);
        this.deletingFolderId = null;
        this.cdr.detectChanges();
      }
    });
  }

  // Manage move to folder
  showMoveToFolderModal = false;
  movingConvId: string | null = null;
  isMovingConv = false;

  openMoveToFolderModal(id: string, event: Event): void {
    event.stopPropagation();
    this.movingConvId = id;
    this.showMoveToFolderModal = true;
  }

  cancelMoveToFolder(): void {
    this.showMoveToFolderModal = false;
    this.movingConvId = null;
  }

  executeMoveToFolder(folderId: string | null): void {
    if (!this.movingConvId || this.isMovingConv) return;
    const convId = this.movingConvId;
    this.isMovingConv = true;

    this.chatService.moveConversationToFolder(convId, folderId).subscribe({
      next: () => {
        this.isMovingConv = false;
        this.showMoveToFolderModal = false;

        // Update local state
        const conv = this.conversationsList.find(c => c.id === convId);
        if (conv) {
          const oldFolderId = conv.folderId;
          conv.folderId = folderId;

          // Remove from old folder
          if (oldFolderId) {
            const oldF = this.folders.find(f => f.id === oldFolderId);
            if (oldF && oldF.conversations) {
              oldF.conversations = oldF.conversations.filter(c => c.id !== convId);
            }
          }

          // Add to new folder
          if (folderId) {
            const newF = this.folders.find(f => f.id === folderId);
            if (newF) {
              if (!newF.conversations) newF.conversations = [];
              newF.conversations.push(conv);
            }
          }
        }
        this.movingConvId = null;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isMovingConv = false;
        this.showMoveToFolderModal = false;
        console.error('Error moving conversation', err);
        this.cdr.detectChanges();
      }
    });
  }

  // --- Bookmarks Logic ---
  
  loadBookmarks(): void {
    const saved = localStorage.getItem('safarni_bookmarks');
    if (saved) {
      try {
        this.bookmarks = JSON.parse(saved);
      } catch (e) {
        this.bookmarks = [];
      }
    }
  }

  saveBookmarks(): void {
    localStorage.setItem('safarni_bookmarks', JSON.stringify(this.bookmarks));
  }

  toggleBookmark(m: ChatMessage): void {
    if (m.isBookmarked) {
      this.removeBookmark(m.id);
      m.isBookmarked = false;
    } else {
      m.isBookmarked = true;
      // We store a copy of the message without the formatted object (to save space) or just re-format it later
      // But storing formatted might be easier for the modal
      this.bookmarks.unshift({ ...m, isBookmarked: true });
      this.saveBookmarks();
    }
  }

  removeBookmark(id: string): void {
    this.bookmarks = this.bookmarks.filter(b => b.id !== id);
    this.saveBookmarks();
    
    // Update current messages if the removed bookmark is in the view
    const msg = this.messages.find(m => m.id === id);
    if (msg) {
      msg.isBookmarked = false;
    }
  }

  openBookmarks(): void {
    this.isBookmarksModalOpen = true;
  }

  closeBookmarks(): void {
    this.isBookmarksModalOpen = false;
  }

  // --- API Integrations ---

  handleSend(): void {
    if (this.isTyping) return;

    const text = this.inputValue.trim();
    if (!text) return;

    if (text.length > this.maxMessageLength) {
      this.addErrorMessage(`Message exceeds ${this.maxMessageLength} characters. Please shorten it.`);
      return;
    }

    this.messages.push(this.toChatMessage(text, true));
    this.inputValue = '';
    this.resetTextareaHeight();
    this.shouldScroll = true;
    this.startThinkingAnimation();
    this.renderNow();

    if (!this.conversationId) {
      const title = text.length > 32 ? `${text.substring(0, 32)}...` : text;
      this.chatService.startConversation(title).subscribe({
        next: (conv) => {
          this.zone.run(() => {
            this.conversationId = conv.id;
            localStorage.setItem('active_conversation_id', conv.id);
            this.renderNow();
            void this.streamMessage(conv.id, text, this.activeRequestId);
          });
        },
        error: (err) => {
          this.zone.run(() => {
            this.stopThinkingAnimation();
            console.error('Error starting conversation', err);
            this.addErrorMessage('Unable to start chat. Check connection and try again.');
            this.renderNow();
          });
        }
      });
      return;
    }

    void this.streamMessage(this.conversationId, text, this.activeRequestId);
  }

  onTextareaInput(event: Event): void {
    const textarea = event.target as HTMLTextAreaElement;
    if (textarea) {
      textarea.style.height = 'auto';
      textarea.style.height = `${Math.min(textarea.scrollHeight, 140)}px`;
    }
  }

  onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.handleSend();
    }
  }

  private resetTextareaHeight(): void {
    if (this.textareaRef) {
      this.textareaRef.nativeElement.style.height = 'auto';
    }
  }

  stopResponse(): void {
    this.activeRequestId++;

    if (this.messageSubscription) {
      this.messageSubscription.unsubscribe();
      this.messageSubscription = undefined;
    }

    this.abortController?.abort();
    this.abortController = null;
    this.streamingMessageIndex = null;

    this.stopThinkingAnimation();
    this.clearTypewriter();
  }

  stopGenerating(): void {
    if (this.isTyping) {
      this.stopResponse();
    }
    const streamingMsg = this.messages.find(m => m.isStreaming);
    if (streamingMsg) {
      this.clearTypewriter();
      streamingMsg.isStreaming = false;
      this.cdr.detectChanges();
    }
  }

  handleRefresh(): void {
    this.startNewConversation();
  }

  toggleSidebar(): void {
    this.isSidebarOpen = !this.isSidebarOpen;
  }

  useSuggestion(text: string): void {
    if (this.isTyping) return;
    this.inputValue = text;
    this.handleSend();
  }

  executeActionChip(chip: ActionChip): void {
    if (chip.actionType === 'planner') {
      this.router.navigate(['/planner'], { queryParams: { trip: chip.promptText } });
    } else {
      this.useSuggestion(chip.promptText);
    }
  }

  // --- Message Controls & Toolbar Methods ---

  copyMessage(m: ChatMessage): void {
    if (m.copied) return;
    navigator.clipboard.writeText(m.text).then(() => {
      m.copied = true;
      this.cdr.detectChanges();
      setTimeout(() => {
        m.copied = false;
        this.cdr.detectChanges();
      }, 2000);
    });
  }

  toggleFeedback(m: ChatMessage, type: 'like' | 'dislike'): void {
    if (m.feedback === type) {
      m.feedback = null;
    } else {
      m.feedback = type;
    }
    this.cdr.detectChanges();
    // Here you would typically send an API request to log the feedback
  }

  startEdit(m: ChatMessage): void {
    m.isEditing = true;
    m.editText = m.text;
  }

  cancelEdit(m: ChatMessage): void {
    m.isEditing = false;
    m.editText = undefined;
  }

  saveEditAndResend(m: ChatMessage): void {
    if (!m.editText || !m.editText.trim() || this.isTyping || !this.conversationId || !m.id) return;

    const newText = m.editText.trim();
    const messageId = m.id;
    m.isEditing = false;
    m.editText = undefined;
    
    // Optimistically update the UI to show thinking state
    m.text = newText;
    this.isTyping = true;
    this.startThinkingAnimation();
    
    this.chatService.editMessage(this.conversationId, messageId, newText).subscribe({
      next: (conv) => {
        this.zone.run(() => {
          this.stopThinkingAnimation();
          this.isTyping = false;
          this.updateMessagesWithStreaming(conv);
          this.refreshConversations();
          this.shouldScroll = true;
          this.renderNow();
        });
      },
      error: (err) => {
        console.error('Error editing message:', err);
        this.zone.run(() => {
          this.stopThinkingAnimation();
          this.isTyping = false;
        });
      }
    });
  }

  regenerateAiResponse(m: ChatMessage): void {
    if (this.isTyping || !this.conversationId || !this.isLatestAssistantMessage(m)) return;

    m.isRegenerating = true;
    this.startThinkingAnimation();

    this.chatService.regenerateResponse(this.conversationId).subscribe({
      next: (conv) => {
        this.zone.run(() => {
          this.stopThinkingAnimation();
          m.isRegenerating = false;

          const updatedMessages = conv.messages;
          const lastAiMsg = updatedMessages[updatedMessages.length - 1];

          if (lastAiMsg && lastAiMsg.role !== 'user') {
            const newText = lastAiMsg.content;
            const fullText = newText;

            m.versions = m.versions || [m.text];
            m.versions.push(newText);
            m.activeVersionIndex = m.versions.length - 1;

            m.text = newText;
            m.isStreaming = true;
            m.streamedText = '';
            m.formatted = this.formatMessage('', false);
            
            let currentIndex = 0;
            this.clearTypewriter();
            this.typewriterInterval = setInterval(() => {
              if (currentIndex < fullText.length) {
                const chunkSize = Math.max(1, Math.floor(fullText.length / 150));
                m.streamedText += fullText.substring(currentIndex, currentIndex + chunkSize);
                currentIndex += chunkSize;
                m.formatted = this.formatMessage(m.streamedText!, false, true);
                this.cdr.detectChanges();
              } else {
                this.clearTypewriter();
                m.isStreaming = false;
                m.streamedText = fullText;
                m.formatted = this.formatMessage(fullText, false);
                this.cdr.detectChanges();
              }
            }, 80);
          }

          this.refreshConversations();
          this.shouldScroll = true;
          this.renderNow();
        });
      },
      error: (err) => {
        this.zone.run(() => {
          this.stopThinkingAnimation();
          m.isRegenerating = false;
          console.error('Failed to regenerate response', err);
          this.addErrorMessage('Unable to regenerate response. Please try again.');
          this.renderNow();
        });
      }
    });
  }

  isLatestAssistantMessage(message: ChatMessage): boolean {
    const latestAssistant = [...this.messages]
      .reverse()
      .find(item => !item.isUser && item.kind === 'normal');

    return latestAssistant === message;
  }

  switchVersion(m: ChatMessage, delta: number): void {
    if (!m.versions || m.versions.length <= 1) return;

    const currentIdx = m.activeVersionIndex || 0;
    const newIdx = currentIdx + delta;

    if (newIdx >= 0 && newIdx < m.versions.length) {
      m.activeVersionIndex = newIdx;
      m.text = m.versions[newIdx];
      m.formatted = this.formatMessage(m.text, false);
      this.cdr.detectChanges();
    }
  }

  // --- Private Helpers ---

  private startThinkingAnimation(): void {
    this.isTyping = true;
    this.renderNow();
  }

  private stopThinkingAnimation(): void {
    this.isTyping = false;
    this.renderNow();
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
            if (requestId !== this.activeRequestId) {
              return;
            }

            this.appendStreamingToken(token);
          },
          onCompleted: (conversation) => {
            if (requestId !== this.activeRequestId) {
              return;
            }

            this.messages = conversation.messages.map(message =>
              this.toChatMessage(message.content, message.role === 'user')
            );

            this.streamingMessageIndex = null;
            this.refreshConversations();
            this.shouldScroll = true;
            this.renderNow();
          }
        },
        controller.signal
      );
    } catch (error: unknown) {
      if (
        requestId !== this.activeRequestId ||
        controller.signal.aborted ||
        (error instanceof DOMException && error.name === 'AbortError')
      ) {
        return;
      }

      console.error('Error streaming chat response', error);
      const errorDetails = this.getSendErrorDetails(error);
      this.addErrorMessage(errorDetails.text, errorDetails.action);
      this.renderNow();
    } finally {
      if (requestId === this.activeRequestId) {
        this.abortController = null;
        this.streamingMessageIndex = null;
        this.stopThinkingAnimation();
      }
    }
  }

  private appendStreamingToken(token: string): void {
    if (this.streamingMessageIndex === null) {
      const message = this.toChatMessage(token, false);

      this.messages.push(message);
      this.streamingMessageIndex = this.messages.length - 1;
    } else {
      const message = this.messages[this.streamingMessageIndex];
      message.text += token;
      message.formatted = this.formatMessage(message.text, false);
    }

    this.shouldScroll = true;
    this.renderNow();
  }

  private sendMessage(convId: string, text: string): void {
    this.messageSubscription = this.chatService.sendMessage(convId, text).subscribe({
      next: (conv) => {
        this.zone.run(() => {
          this.stopThinkingAnimation();
          this.messageSubscription = undefined;
          this.updateMessagesWithStreaming(conv);
          this.refreshConversations();
          this.shouldScroll = true;
          this.renderNow();
        });
      },
      error: (err) => {
        this.zone.run(() => {
          this.stopThinkingAnimation();
          this.messageSubscription = undefined;
          console.error('Error sending message', err);
          const errorDetails = this.getSendErrorDetails(err);
          this.addErrorMessage(errorDetails.text, errorDetails.action);
          this.renderNow();
        });
      }
    });
  }

  private renderNow(): void {
    const shouldScrollAfterRender = this.shouldScroll;
    this.cdr.detectChanges();

    if (shouldScrollAfterRender) {
      window.setTimeout(() => {
        this.scrollMessagesToBottom();
      }, 0);
    }
  }

  private clearTypewriter(): void {
    if (this.typewriterInterval) {
      clearInterval(this.typewriterInterval);
      this.typewriterInterval = undefined;
    }
  }

  private updateMessagesWithStreaming(conv: Conversation): void {
    this.clearTypewriter();
    const newMessages = conv.messages.map(m => this.toChatMessage(m.content, m.role === 'user', 'normal', m.id));
    const lastMsg = newMessages[newMessages.length - 1];

    if (lastMsg && !lastMsg.isUser && lastMsg.kind === 'normal') {
      lastMsg.isStreaming = true;
      lastMsg.streamedText = '';
      lastMsg.formatted = this.formatMessage('', false);
      const fullText = lastMsg.text;
      
      this.messages = newMessages;
      let currentIndex = 0;
      
      this.typewriterInterval = setInterval(() => {
        if (currentIndex < fullText.length) {
          const chunkSize = Math.max(1, Math.floor(fullText.length / 150));
          lastMsg.streamedText += fullText.substring(currentIndex, currentIndex + chunkSize);
          currentIndex += chunkSize;
          lastMsg.formatted = this.formatMessage(lastMsg.streamedText!, false, true);
          this.cdr.detectChanges();
        } else {
          this.clearTypewriter();
          lastMsg.isStreaming = false;
          lastMsg.streamedText = fullText;
          lastMsg.formatted = this.formatMessage(fullText, false);
          this.cdr.detectChanges();
        }
      }, 80);
    } else {
      this.messages = newMessages;
    }
  }

  private scrollMessagesToBottom(): void {
    const el = this.chatMessagesRef?.nativeElement;
    if (!el) {
      return;
    }

    el.scrollTop = el.scrollHeight;
    this.shouldScroll = false;
  }

  private refreshConversations(): void {
    this.chatService.getConversations().subscribe({
      next: (list) => {
        this.conversationsList = list || [];
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Failed to refresh conversation list', err)
    });
  }

  private getSendErrorDetails(error: any): { text: string; action?: 'new-chat' } {
    if (error?.name === 'TimeoutError') {
      return { text: 'The request took longer than expected. Please try again with a shorter question.' };
    }
    if (error?.status === 0) {
      return { text: 'Unable to connect to the travel service. Please check your connection and try again.' };
    }
    if (error?.status === 429) {
      return {
        text: 'وصلت هذه المحادثة إلى الحد المتاح حاليًا. ابدأ شات جديد للمتابعة.',
        action: 'new-chat'
      };
    }
    return { text: 'An unexpected error occurred while preparing your travel plan. Please try again.' };
  }

  private addErrorMessage(text: string, action?: 'new-chat'): void {
    const message = this.toChatMessage(text, false, 'error');
    message.action = action;
    this.messages.push(message);
    this.shouldScroll = true;
  }

  private toChatMessage(text: string, isUser: boolean, kind: 'normal' | 'error' = 'normal', id?: string): ChatMessage {
    const isBookmarked = this.bookmarks.some(b => b.id === id);

    return {
      id: id || Math.random().toString(36).substring(2, 9),
      text,
      isUser,
      time: this.nowLabel(),
      kind,
      formatted: this.formatMessage(text, isUser),
      versions: isUser ? undefined : [text],
      activeVersionIndex: isUser ? undefined : 0,
      isBookmarked
    };
  }

  private nowLabel(): string {
    return new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  }

  private formatMessage(text: string, isUser: boolean, isStreaming: boolean = false): FormattedMessage {
    let suggestionsBlock = '';
    let textToProcess = text;

    if (!isUser) {
      const suggestionMatch = textToProcess.match(/---SUGGESTIONS---([\s\S]*)$/i);
      if (suggestionMatch) {
        suggestionsBlock = suggestionMatch[1].trim();
        textToProcess = textToProcess.substring(0, suggestionMatch.index).trim();
      }
    }

    const normalized = this.normalizeAssistantText(textToProcess);
    const links = this.extractLinks(normalized);
    const withoutUrls = normalized.replace(this.urlPattern, '').trim();
    let cleaned = this.cleanMarkdown(withoutUrls);

    // AI Response specific parsing
    let summary: string | undefined = undefined;
    let itineraryDays: ItineraryDay[] = [];
    let actionChips: ActionChip[] = [];

    if (!isUser && !isStreaming) {
      itineraryDays = this.parseItineraryDays(cleaned);
      actionChips = this.generateActionChips(cleaned, itineraryDays, suggestionsBlock);
      
      // Remove itinerary text from paragraphs so it doesn't double render
      if (itineraryDays.length > 0) {
        const dayRegex = /(?:Day|اليوم)\s*(\d+)[:\s–-]*(.*?)(?=(?:Day|اليوم)\s*\d+|$)/gsi;
        cleaned = cleaned.replace(dayRegex, '').trim();
      }
    }

    const direction = this.detectDirection(cleaned);
    const paragraphs = this.extractParagraphs(cleaned);

    if (!isUser && !isStreaming) {
      const hasSubstantialTravelResponse = paragraphs.length >= 3 && cleaned.length >= 280;
      if (hasSubstantialTravelResponse) {
        summary = paragraphs[0];
      }
    }

    return {
      direction,
      summary,
      paragraphs: summary ? paragraphs.slice(1) : paragraphs,
      links,
      itineraryDays,
      actionChips
    };
  }

  private detectDirection(text: string): 'rtl' | 'ltr' {
    return /[\u0600-\u06FF]/.test(text) ? 'rtl' : 'ltr';
  }

  private normalizeAssistantText(text: string): string {
    return text
      .replace(/\r\n/g, '\n')
      .replace(/\\n/g, '\n')
      .replace(/<br\s*\/?>/gi, '\n')
      .replace(/<\/li>\s*<li[^>]*>/gi, '\n')
      .replace(/<\/?(?:ul|ol)[^>]*>/gi, '\n')
      .replace(/<\/?(?:p|div|span|strong|b|em|i|h[1-6])[^>]*>/gi, ' ')
      .replace(/&nbsp;/gi, ' ')
      .replace(/&amp;/gi, '&')
      .replace(/&lt;/gi, '<')
      .replace(/&gt;/gi, '>')
      .replace(/\n{3,}/g, '\n\n')
      .trim();
  }

  private cleanMarkdown(text: string): string {
    return text
      .replace(/^#{1,6}\s*/gm, '')
      .replace(/\*\*(.*?)\*\*/g, '$1')
      .replace(/__(.*?)__/g, '$1')
      .replace(/`{1,3}([^`]+)`{1,3}/g, '$1')
      .replace(/\[(.*?)\]\((.*?)\)/g, '$1')
      .replace(/^\s*[-*]\s+/gm, '• ')
      .replace(/[ \t]+/g, ' ')
      .replace(/\n{3,}/g, '\n\n')
      .trim();
  }

  private parseItineraryDays(text: string): ItineraryDay[] {
    const days: ItineraryDay[] = [];
    const dayRegex = /(?:Day|اليوم)\s*(\d+)[:\s–-]*(.*?)(?=(?:Day|اليوم)\s*\d+|$)/gsi;
    let match: RegExpExecArray | null;

    while ((match = dayRegex.exec(text)) !== null) {
      const dayNumber = parseInt(match[1], 10);
      const content = match[2].trim();

      if (dayNumber && content) {
        days.push({
          dayNumber,
          title: content.split('\n')[0].substring(0, 60),
          morning: this.extractTimeSlot(content, ['morning', 'صباحاً', 'صباح']),
          afternoon: this.extractTimeSlot(content, ['afternoon', 'ظهراً', 'عصراً']),
          evening: this.extractTimeSlot(content, ['evening', 'night', 'مساءً', 'ليلاً']),
          budget: this.extractBudgetSlot(content)
        });
      }
    }

    return days.slice(0, 7); // Cap at 7 days max
  }

  private extractTimeSlot(content: string, keywords: string[]): string | undefined {
    const lines = content.split('\n');
    for (const line of lines) {
      if (keywords.some(kw => line.toLowerCase().includes(kw))) {
        return line.replace(/^[-•*\s]+/, '').trim();
      }
    }
    return undefined;
  }

  private extractBudgetSlot(content: string): string | undefined {
    const match = content.match(/(\$\d+|\d+\s*USD|\d+\s*جنيه|\d+\s*دولار)/i);
    return match ? match[1] : undefined;
  }

  private generateActionChips(text: string, itineraryDays: ItineraryDay[], suggestionsBlock: string = ''): ActionChip[] {
    const chips: ActionChip[] = [];

    // Process Explicit Suggestions from AI
    if (suggestionsBlock) {
      const lines = suggestionsBlock.split('\n');
      for (const line of lines) {
        const cleanLine = line.replace(/^[-*•\d.)]+\s*/, '').trim();
        if (cleanLine) {
          chips.push({
            label: cleanLine.length > 40 ? cleanLine.substring(0, 37) + '...' : cleanLine,
            icon: 'lightbulb',
            promptText: cleanLine,
            actionType: 'prompt'
          });
        }
      }
    }

    // Strictly exclude fallback, out-of-scope, or error messages from getting itinerary chips
    if (/(?:خارج نطاق|خارج التخصص|تعذر|حدث خطأ|ليس لدي معلومات)/i.test(text)) {
      return chips;
    }

    // Require parsed day cards or explicit structured itinerary markers for Planner chips
    const isExplicitItinerary = itineraryDays.length > 0 ||
      /(?:Day\s*\d|اليوم\s*\d|خطة الرحلة|جدول الرحلة|البرنامج المقترح|Suggested Itinerary|Day-by-Day Plan)/i.test(text);

    if (isExplicitItinerary) {
      const isArabic = this.detectDirection(text) === 'rtl';

      chips.push(
        {
          label: isArabic ? 'إنشاء رحلة بالخطط' : 'Create Trip from this',
          icon: 'map',
          promptText: isArabic ? 'قم بإنشاء رحلة تفصيلية بناءً على هذا الرد' : 'Create a detailed trip in Planner based on this recommendation',
          actionType: 'planner'
        },
        {
          label: isArabic ? 'ميزانية أقل' : 'Make it cheaper',
          icon: 'savings',
          promptText: isArabic ? 'كيف يمكن تقليل ميزانية هذه الرحلة؟' : 'How can I lower the budget for this itinerary?',
          actionType: 'prompt'
        }
      );
    }

    return chips;
  }

  private extractLinks(text: string): MessageLink[] {
    const matches = text.match(this.urlPattern) || [];
    const isArabic = this.detectDirection(text) === 'rtl';
    const seen = new Set<string>();

    return matches
      .map(rawUrl => rawUrl.replace(/[.,،؛]+$/, ''))
      .filter(url => {
        if (seen.has(url)) return false;
        seen.add(url);
        return true;
      })
      .map((url, index) => {
        const host = this.getHostname(url);
        const isMap = /maps\.google|google\.com\/maps|place_id/i.test(url);

        return {
          url,
          icon: isMap ? 'map' : 'open_in_new',
          label: isMap
            ? (isArabic ? 'افتح على الخريطة' : 'View on map')
            : (host || (isArabic ? `رابط ${index + 1}` : `Open Link ${index + 1}`))
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

  private extractParagraphs(text: string): string[] {
    return text
      .split(/\n{2,}|(?=\n\s*(?:[•*-]|\d+[.)])\s+)|(?<=[.!؟])\s+(?=[\u0600-\u06FFA-Z0-9])/)
      .map(p => p.replace(/\s+/g, ' ').trim())
      .filter(Boolean);
  }
}
