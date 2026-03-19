# Rich Text Editor - Usage Examples

This document provides real-world examples of how to use the Rich Text Editor component in different parts of the LIS application.

## Example 1: Patient Results Component (Already Implemented)

### TypeScript
```typescript
import { RichTextEditorComponent } from '../shared/rich-text-editor/rich-text-editor.component';

@Component({
  selector: 'app-patient-results',
  standalone: true,
  imports: [CommonModule, FormsModule, RichTextEditorComponent],
  templateUrl: './patient-results.component.html',
  styleUrls: ['./patient-results.component.scss']
})
export class PatientResultsComponent {
  readonly textResults = computed(() => {
    return this.labResults().filter(r => 
      r.defaultTextResult && r.defaultTextResult.trim() !== ''
    );
  });

  onTextResultBlur(result: PatientLabResult): void {
    console.log('Text result updated:', result.id, result.result);
    // Content is already updated via contentChange event
  }

  saveLabResults(): void {
    // Text results are already updated via the rich text editor component's contentChange event
    const results = this.labResults();
    // Save logic here...
  }
}
```

### HTML
```html
<div *ngFor="let result of textResults()">
  <h6>{{ result.labTestDescription }}</h6>
  <small class="text-muted">{{ result.medicalClassDesc }}</small>
  
  <app-rich-text-editor
    [content]="result.result || result.defaultTextResult || ''"
    [placeholder]="'Enter text result...'"
    [editorId]="'text-result-' + result.id"
    [minHeight]="'150px'"
    [maxHeight]="'400px'"
    [toolbarStyle]="'simple'"
    (contentChange)="result.result = $event"
    (onBlur)="onTextResultBlur(result)">
  </app-rich-text-editor>
</div>
```

## Example 2: Lab Tests Component - Default Text Result

### TypeScript
```typescript
import { RichTextEditorComponent } from '../shared/rich-text-editor/rich-text-editor.component';

@Component({
  selector: 'app-lab-tests',
  standalone: true,
  imports: [CommonModule, FormsModule, RichTextEditorComponent],
  templateUrl: './lab-tests.component.html',
  styleUrls: ['./lab-tests.component.scss']
})
export class LabTestsComponent {
  @ViewChild(RichTextEditorComponent) richTextEditor?: RichTextEditorComponent;
  
  readonly editItem = signal<LabTest | null>(null);
  
  isTextResultType(): boolean {
    const item = this.editItem();
    return item?.resultType === 2; // 2 = Text result type
  }

  onDefaultTextResultChange(content: string): void {
    const item = this.editItem();
    if (item) {
      item.defaultTextResult = content;
    }
  }

  onEditorBlur(content: string): void {
    const item = this.editItem();
    if (item) {
      // Clean empty content
      if (content === '<br>' || content === '<p><br></p>' || content.trim() === '') {
        item.defaultTextResult = null;
      } else {
        item.defaultTextResult = content;
      }
    }
  }
}
```

### HTML
```html
<!-- Show rich text editor when Result Type is Text -->
<div *ngIf="isTextResultType()" class="mb-3">
  <label class="form-label">
    Default Text Result
    <small class="text-muted">(Rich text content for text-based results)</small>
  </label>
  
  <app-rich-text-editor
    [content]="editItem()?.defaultTextResult || ''"
    [placeholder]="'Enter default text result...'"
    [editorId]="'default-text-result'"
    [minHeight]="'200px'"
    [maxHeight]="'500px'"
    [toolbarStyle]="'full'"
    (contentChange)="onDefaultTextResultChange($event)"
    (onBlur)="onEditorBlur($event)">
  </app-rich-text-editor>
</div>
```

## Example 3: Comments/Notes Section

For adding rich text comments or notes anywhere in your application:

### TypeScript
```typescript
import { RichTextEditorComponent } from '../shared/rich-text-editor/rich-text-editor.component';

@Component({
  selector: 'app-notes',
  standalone: true,
  imports: [CommonModule, FormsModule, RichTextEditorComponent],
  template: `
    <div class="notes-section">
      <h5>Notes</h5>
      <app-rich-text-editor
        [(content)]="notes"
        [placeholder]="'Add your notes here...'"
        [minHeight]="'150px'"
        [maxHeight]="'400px'"
        [toolbarStyle]="'simple'"
        (onBlur)="saveNotes($event)">
      </app-rich-text-editor>
    </div>
  `
})
export class NotesComponent {
  notes = '';

  saveNotes(content: string): void {
    // Auto-save notes when editor loses focus
    this.http.post('/api/notes', { content }).subscribe();
  }
}
```

## Example 4: Multiple Editors with Dynamic Content

### TypeScript
```typescript
import { RichTextEditorComponent } from '../shared/rich-text-editor/rich-text-editor.component';

@Component({
  selector: 'app-multi-editor',
  standalone: true,
  imports: [CommonModule, FormsModule, RichTextEditorComponent],
  template: `
    <div *ngFor="let section of sections; let i = index">
      <h6>{{ section.title }}</h6>
      <app-rich-text-editor
        [content]="section.content"
        [placeholder]="'Enter ' + section.title.toLowerCase() + '...'"
        [editorId]="'section-' + i"
        [minHeight]="'100px'"
        [maxHeight]="'300px'"
        [toolbarStyle]="'simple'"
        (contentChange)="section.content = $event">
      </app-rich-text-editor>
    </div>
  `
})
export class MultiEditorComponent {
  sections = [
    { title: 'Introduction', content: '' },
    { title: 'Findings', content: '' },
    { title: 'Conclusion', content: '' }
  ];
}
```

## Example 5: With ViewChild Reference

Access editor methods programmatically:

### TypeScript
```typescript
import { ViewChild } from '@angular/core';
import { RichTextEditorComponent } from '../shared/rich-text-editor/rich-text-editor.component';

@Component({
  selector: 'app-with-reference',
  template: `
    <app-rich-text-editor #editor></app-rich-text-editor>
    <button (click)="loadTemplate()">Load Template</button>
    <button (click)="getContent()">Get Content</button>
    <button (click)="clearContent()">Clear</button>
  `
})
export class WithReferenceComponent {
  @ViewChild('editor') editor!: RichTextEditorComponent;

  loadTemplate(): void {
    const template = '<h3>Report</h3><p>Enter your findings here...</p>';
    this.editor.updateContent(template);
  }

  getContent(): void {
    const content = this.editor.getContent();
    console.log('Current content:', content);
  }

  clearContent(): void {
    this.editor.updateContent('');
  }
}
```

## Tips and Best Practices

### 1. Choose the Right Toolbar Style
- **Full Toolbar**: Use for main content editing (lab test definitions, rich documents)
- **Simple Toolbar**: Use for quick notes, comments, or when space is limited

### 2. Handle Content Updates
Always use the `contentChange` event to capture changes:
```html
<app-rich-text-editor
  [content]="myContent"
  (contentChange)="myContent = $event">
</app-rich-text-editor>
```

### 3. Clean Empty Content
Handle empty content gracefully:
```typescript
onEditorBlur(content: string): void {
  if (content === '<br>' || content === '<p><br></p>' || content.trim() === '') {
    this.item.content = null; // or ''
  } else {
    this.item.content = content;
  }
}
```

### 4. Unique Editor IDs
Always provide unique IDs when using multiple editors:
```html
<app-rich-text-editor
  [editorId]="'editor-' + uniqueId">
</app-rich-text-editor>
```

### 5. Responsive Heights
Adjust heights based on use case:
- Quick notes: `minHeight="100px"` `maxHeight="200px"`
- Standard editing: `minHeight="150px"` `maxHeight="400px"`
- Full document: `minHeight="200px"` `maxHeight="600px"`

### 6. Loading Existing Content
Initialize content in ngAfterViewInit or after data loads:
```typescript
ngAfterViewInit(): void {
  this.loadData().subscribe(data => {
    // Content will be automatically displayed
    this.content = data.richTextContent;
  });
}
```

### 7. Form Integration
The component works seamlessly with reactive forms:
```typescript
form = new FormGroup({
  content: new FormControl('')
});

<app-rich-text-editor
  [content]="form.get('content')?.value"
  (contentChange)="form.get('content')?.setValue($event)">
</app-rich-text-editor>
```

## Common Issues and Solutions

### Issue: Content Not Updating
**Solution**: Make sure you're using the `contentChange` event:
```html
(contentChange)="myContent = $event"
```

### Issue: Multiple Editors Interfering
**Solution**: Provide unique `editorId` for each editor:
```html
[editorId]="'editor-' + index"
```

### Issue: Content Lost on Navigation
**Solution**: Save content in `onBlur` event:
```html
(onBlur)="saveContent($event)"
```

### Issue: Toolbar Not Showing
**Solution**: Ensure `showToolbar` is true (default):
```html
[showToolbar]="true"
```


