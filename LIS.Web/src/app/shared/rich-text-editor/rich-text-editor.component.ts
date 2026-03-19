import { Component, Input, Output, EventEmitter, signal, AfterViewInit, ElementRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-rich-text-editor',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './rich-text-editor.component.html',
  styleUrls: ['./rich-text-editor.component.scss']
})
export class RichTextEditorComponent implements AfterViewInit {
  @ViewChild('editor') editorElement?: ElementRef<HTMLElement>;

  @Input() content: string = '';
  @Input() placeholder: string = 'Enter text...';
  @Input() editorId: string = 'rich-text-editor-' + Math.random().toString(36).substr(2, 9);
  @Input() minHeight: string = '150px';
  @Input() maxHeight: string = '400px';
  @Input() showToolbar: boolean = true;
  @Input() toolbarStyle: 'full' | 'simple' = 'full';

  @Output() contentChange = new EventEmitter<string>();
  @Output() onFocus = new EventEmitter<void>();
  @Output() onBlur = new EventEmitter<string>();

  private savedSelection: Range | null = null;
  readonly showTablePicker = signal(false);
  readonly showLinkDialog = signal(false);
  readonly linkUrl = signal('');
  readonly linkText = signal('');

  // Formatting state tracking
  readonly isBold = signal(false);
  readonly isItalic = signal(false);
  readonly isUnderline = signal(false);
  readonly isStrikeThrough = signal(false);

  ngAfterViewInit(): void {
    // Initialize content
    if (this.editorElement && this.content) {
      this.editorElement.nativeElement.innerHTML = this.content;
    }
  }

  saveSelection(): void {
    const selection = window.getSelection();
    if (selection && selection.rangeCount > 0) {
      this.savedSelection = selection.getRangeAt(0);
    }
  }

  restoreSelection(): void {
    const editor = this.editorElement?.nativeElement;
    if (!editor) return;

    editor.focus();

    if (this.savedSelection) {
      const selection = window.getSelection();
      if (selection) {
        selection.removeAllRanges();
        selection.addRange(this.savedSelection);
      }
    }
  }

  execCommand(command: string, value?: string): void {
    const editor = this.editorElement?.nativeElement;
    if (!editor) return;

    this.restoreSelection();

    const result = document.execCommand(command, false, value);

    if (!result) {
      console.warn(`Command ${command} failed to execute`);
    }

    this.saveSelection();
    this.updateFormattingState();
    this.emitContentChange();
  }

  formatBlock(tag: string): void {
    this.execCommand('formatBlock', tag);
  }

  onFormatBlockChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    const value = select.value;
    if (value) {
      this.formatBlock(value);
    }
    select.selectedIndex = 0;
  }

  onToolbarMouseDown(event: MouseEvent): void {
    event.preventDefault();
  }

  onEditorInput(event: Event): void {
    this.saveSelection();
    this.updateFormattingState();
    this.emitContentChange();
  }

  onEditorFocus(): void {
    this.onFocus.emit();
  }

  onEditorBlur(): void {
    const editor = this.editorElement?.nativeElement;
    if (editor) {
      const content = editor.innerHTML;
      this.onBlur.emit(content);
    }
  }

  updateFormattingState(): void {
    this.isBold.set(document.queryCommandState('bold'));
    this.isItalic.set(document.queryCommandState('italic'));
    this.isUnderline.set(document.queryCommandState('underline'));
    this.isStrikeThrough.set(document.queryCommandState('strikeThrough'));
  }

  isFormatActive(command: string): boolean {
    return document.queryCommandState(command);
  }

  insertLink(): void {
    this.showLinkDialog.set(true);
    this.linkUrl.set('');
    this.linkText.set('');
  }

  applyLink(): void {
    const url = this.linkUrl();
    const text = this.linkText();

    if (url) {
      this.restoreSelection();

      if (text) {
        const selection = window.getSelection();
        if (selection && selection.rangeCount > 0) {
          const range = selection.getRangeAt(0);
          range.deleteContents();
          const textNode = document.createTextNode(text);
          range.insertNode(textNode);
          range.selectNode(textNode);
          selection.removeAllRanges();
          selection.addRange(range);
        }
      }

      this.execCommand('createLink', url);
    }

    this.showLinkDialog.set(false);
    this.emitContentChange();
  }

  cancelLink(): void {
    this.showLinkDialog.set(false);
  }

  toggleTablePicker(event?: Event): void {
    event?.stopPropagation();
    this.showTablePicker.set(!this.showTablePicker());
  }

  insertTable(rows: number, cols: number): void {
    let tableHTML = '<table class="table table-bordered"><tbody>';
    for (let i = 0; i < rows; i++) {
      tableHTML += '<tr>';
      for (let j = 0; j < cols; j++) {
        tableHTML += '<td>&nbsp;</td>';
      }
      tableHTML += '</tr>';
    }
    tableHTML += '</tbody></table>';

    const editor = this.editorElement?.nativeElement;
    if (editor) {
      this.restoreSelection();

      try {
        document.execCommand('insertHTML', false, tableHTML);
      } catch (e) {
        editor.innerHTML += tableHTML;
      }

      this.saveSelection();
      this.emitContentChange();
    }

    this.showTablePicker.set(false);
  }

  changeTextColor(event: Event): void {
    const input = event.target as HTMLInputElement;
    const color = input.value;

    const selection = window.getSelection();
    if (selection && selection.toString().length > 0) {
      this.execCommand('foreColor', color);
    }
  }

  changeBackgroundColor(event: Event): void {
    const input = event.target as HTMLInputElement;
    const color = input.value;

    const selection = window.getSelection();
    if (selection && selection.toString().length > 0) {
      let success = document.execCommand('hiliteColor', false, color);
      if (!success) {
        document.execCommand('backColor', false, color);
      }
      this.emitContentChange();
    }
  }

  private emitContentChange(): void {
    const editor = this.editorElement?.nativeElement;
    if (editor) {
      this.contentChange.emit(editor.innerHTML);
    }
  }

  // Public method to update content programmatically
  updateContent(content: string): void {
    if (this.editorElement) {
      this.editorElement.nativeElement.innerHTML = content;
      this.content = content;
    }
  }

  // Public method to get current content
  getContent(): string {
    return this.editorElement?.nativeElement.innerHTML || '';
  }
}


