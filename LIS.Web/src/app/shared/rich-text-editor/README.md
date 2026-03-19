# Rich Text Editor Component

A reusable, feature-rich WYSIWYG text editor component for Angular applications.

## Features

- **Text Formatting**: Bold, Italic, Underline, Strikethrough
- **Text Alignment**: Left, Center, Right
- **Lists**: Bullet and Numbered lists
- **Headings**: H1-H6 support
- **Colors**: Text and background color picker
- **Links**: Insert and edit hyperlinks
- **Tables**: Visual table picker for quick insertion
- **Cleanup**: Remove formatting option
- **Two Toolbar Styles**: Full-featured or simplified toolbar
- **Customizable**: Configurable height, placeholder, and more

## Usage

### Basic Usage

```typescript
import { RichTextEditorComponent } from './shared/rich-text-editor/rich-text-editor.component';

@Component({
  selector: 'app-my-component',
  standalone: true,
  imports: [RichTextEditorComponent],
  template: `
    <app-rich-text-editor
      [(content)]="myContent"
      (contentChange)="onContentChange($event)">
    </app-rich-text-editor>
  `
})
export class MyComponent {
  myContent = '<p>Initial content</p>';

  onContentChange(newContent: string) {
    console.log('Content changed:', newContent);
  }
}
```

### Advanced Usage with All Options

```typescript
<app-rich-text-editor
  [content]="myContent"
  [placeholder]="'Enter your text here...'"
  [editorId]="'my-custom-editor'"
  [minHeight]="'200px'"
  [maxHeight]="'600px'"
  [showToolbar]="true"
  [toolbarStyle]="'full'"
  (contentChange)="onContentChange($event)"
  (onFocus)="onEditorFocus()"
  (onBlur)="onEditorBlur($event)">
</app-rich-text-editor>
```

## Input Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `content` | `string` | `''` | Initial HTML content of the editor |
| `placeholder` | `string` | `'Enter text...'` | Placeholder text shown when editor is empty |
| `editorId` | `string` | Auto-generated | Unique ID for the editor element |
| `minHeight` | `string` | `'150px'` | Minimum height of the editor |
| `maxHeight` | `string` | `'400px'` | Maximum height of the editor (scrolls after) |
| `showToolbar` | `boolean` | `true` | Whether to show the formatting toolbar |
| `toolbarStyle` | `'full' \| 'simple'` | `'full'` | Toolbar style: full features or simplified |

## Output Events

| Event | Type | Description |
|-------|------|-------------|
| `contentChange` | `EventEmitter<string>` | Emitted when content changes |
| `onFocus` | `EventEmitter<void>` | Emitted when editor gains focus |
| `onBlur` | `EventEmitter<string>` | Emitted when editor loses focus, includes current content |

## Public Methods

You can access these methods using a ViewChild reference:

```typescript
@ViewChild(RichTextEditorComponent) editor!: RichTextEditorComponent;

// Update content programmatically
this.editor.updateContent('<p>New content</p>');

// Get current content
const content = this.editor.getContent();
```

### Available Methods

- `updateContent(content: string)`: Update editor content programmatically
- `getContent()`: Get current HTML content from the editor

## Toolbar Styles

### Full Toolbar
Includes all features: formatting, alignment, colors, lists, links, tables, etc.

```html
<app-rich-text-editor [toolbarStyle]="'full'"></app-rich-text-editor>
```

### Simple Toolbar
Includes only basic formatting: bold, italic, underline, lists, headings.

```html
<app-rich-text-editor [toolbarStyle]="'simple'"></app-rich-text-editor>
```

## Examples

### Example 1: Simple Note Editor

```typescript
@Component({
  template: `
    <app-rich-text-editor
      [(content)]="note"
      [placeholder]="'Write your note...'"
      [toolbarStyle]="'simple'"
      [minHeight]="'100px'"
      [maxHeight]="'300px'">
    </app-rich-text-editor>
  `
})
export class NoteComponent {
  note = '';
}
```

### Example 2: Lab Test Results Editor

```typescript
@Component({
  template: `
    <app-rich-text-editor
      [content]="labTest.defaultTextResult || ''"
      [placeholder]="'Enter lab test result...'"
      [editorId]="'lab-test-' + labTest.id"
      [minHeight]="'200px'"
      (contentChange)="labTest.defaultTextResult = $event"
      (onBlur)="saveLabTest()">
    </app-rich-text-editor>
  `
})
export class LabTestComponent {
  labTest: LabTest;
  
  saveLabTest() {
    // Save logic here
  }
}
```

### Example 3: Multiple Editors on Same Page

```typescript
@Component({
  template: `
    <div *ngFor="let result of textResults()">
      <h6>{{ result.labTestDescription }}</h6>
      <app-rich-text-editor
        [content]="result.result || result.defaultTextResult || ''"
        [editorId]="'result-' + result.id"
        [toolbarStyle]="'simple'"
        (contentChange)="result.result = $event">
      </app-rich-text-editor>
    </div>
  `
})
export class PatientResultsComponent {
  textResults = signal<PatientLabResult[]>([]);
}
```

## Styling

The component uses Bootstrap classes and icons. Make sure you have Bootstrap CSS and Bootstrap Icons included in your project:

```html
<!-- In your index.html or angular.json -->
<link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css" rel="stylesheet">
<link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.10.0/font/bootstrap-icons.css" rel="stylesheet">
```

## Browser Compatibility

This component uses the `document.execCommand` API, which is supported in all modern browsers but deprecated. For production use, consider migrating to a more modern solution like:
- ProseMirror
- Quill
- TipTap

However, `execCommand` still works in all major browsers and is suitable for internal applications.

## Notes

- Content is stored as HTML
- The component automatically handles selection and cursor position
- Empty content is displayed with a placeholder
- All formatting is preserved when content is saved
- The editor is fully keyboard accessible (Ctrl+B, Ctrl+I, Ctrl+U for formatting)


