import { Component, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { ApiEndpointsService } from '../api/api-endpoints.service';

@Component({
  selector: 'app-gemini-chat',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './gemini-chat.component.html',
  styleUrl: './gemini-chat.component.scss'
})
export class GeminiChatComponent {
  private http = inject(HttpClient);
  private readonly apiUrl = inject(ApiEndpointsService).chatGpt;

  // Using regular properties for two-way binding with forms
  promptText = '';
  readonly response = signal('');
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly conversationHistory = signal<Array<{ role: 'user' | 'assistant', content: string, timestamp: Date }>>([]);

  sendPrompt(): void {
    const promptText = this.promptText.trim();
    if (!promptText) {
      this.errorMessage.set('Please enter a prompt');
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.response.set('');

    // Add user message to history
    this.conversationHistory.update(history => [
      ...history,
      { role: 'user', content: promptText, timestamp: new Date() }
    ]);

    this.http.post<{ response: string }>(`${this.apiUrl}/chat`, { prompt: promptText }).subscribe({
      next: (data) => {
        this.response.set(data.response);
        // Add assistant response to history
        this.conversationHistory.update(history => [
          ...history,
          { role: 'assistant', content: data.response, timestamp: new Date() }
        ]);
        this.isLoading.set(false);
        // Clear prompt after successful send
        this.promptText = '';
      },
      error: (err) => {
        console.error('Error calling AI API:', err);
        const errorMsg = err.error?.message || err.message || 'Failed to get response from AI Assistant. Please check if the Gemini API key is configured in appsettings.json. Get a free key from https://aistudio.google.com/app/apikey';
        this.errorMessage.set(errorMsg);
        this.isLoading.set(false);
      }
    });
  }

  clearConversation(): void {
    this.conversationHistory.set([]);
    this.response.set('');
    this.promptText = '';
    this.errorMessage.set(null);
  }

  formatTimestamp(date: Date): string {
    return new Date(date).toLocaleString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit'
    });
  }
}

