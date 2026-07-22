import { ChangeDetectionStrategy, Component, effect, ElementRef, input, output, signal, viewChild } from '@angular/core';

@Component({
  selector: 'app-file-upload',
  imports: [],
  templateUrl: './file-upload.html',
  styleUrl: './file-upload.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FileUpload {
  readonly accept = input('.png,.jpg,.jpeg,.svg,.webp');
  readonly label = input('اختر ملفًا...');
  readonly icon = input('fa-solid fa-image');
  readonly disabled = input(false);
  readonly maxSizeMb = input(5);
  /** Externally-controlled preview (e.g. restored from a saved profile). */
  readonly previewUrl = input<string | null>(null);
  /** Externally-controlled upload progress (0-100), or null when no upload is in flight. */
  readonly uploadProgress = input<number | null>(null);
  readonly shape = input<'square' | 'circle'>('square');
  readonly altText = input('');

  readonly fileSelected = output<File>();
  readonly cleared = output<void>();
  readonly validationError = output<string>();

  private readonly fileInputRef = viewChild<ElementRef<HTMLInputElement>>('fileInput');

  protected readonly localPreviewUrl = signal<string | null>(null);
  protected readonly fileName = signal<string | null>(null);
  protected readonly previewLoadFailed = signal(false);
  protected readonly dragActive = signal(false);

  constructor() {
    effect(() => {
      this.localPreviewUrl();
      this.previewUrl();
      this.previewLoadFailed.set(false);
    });
  }

  protected get resolvedPreview(): string | null {
    if (this.previewLoadFailed()) return null;
    return this.localPreviewUrl() ?? this.previewUrl();
  }

  protected onPreviewError(): void {
    this.previewLoadFailed.set(true);
  }

  protected onFileChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;
    this.handleFile(file);
  }

  protected openFilePicker(): void {
    if (this.disabled()) return;
    this.fileInputRef()?.nativeElement.click();
  }

  protected onDropzoneKeydown(event: KeyboardEvent): void {
    if (event.key !== 'Enter' && event.key !== ' ') return;
    event.preventDefault();
    this.openFilePicker();
  }

  protected onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    if (this.disabled()) return;
    this.dragActive.set(true);
  }

  protected onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.dragActive.set(false);
  }

  protected onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.dragActive.set(false);
    if (this.disabled()) return;

    const file = event.dataTransfer?.files?.[0];
    if (!file) return;
    this.handleFile(file);
  }

  private handleFile(file: File): void {
    const error = this.validateFile(file);
    if (error) {
      this.validationError.emit(error);
      return;
    }

    this.fileName.set(file.name);
    const reader = new FileReader();
    reader.onload = e => this.localPreviewUrl.set(e.target?.result as string);
    reader.readAsDataURL(file);

    this.fileSelected.emit(file);
  }

  private validateFile(file: File): string | null {
    const acceptedExtensions = this.accept()
      .split(',')
      .map(ext => ext.trim().toLowerCase())
      .filter(Boolean);
    const fileExtension = `.${file.name.split('.').pop()?.toLowerCase() ?? ''}`;
    if (acceptedExtensions.length > 0 && !acceptedExtensions.includes(fileExtension)) {
      return `صيغة الملف غير مدعومة. الصيغ المسموحة: ${this.accept()}`;
    }

    const maxBytes = this.maxSizeMb() * 1024 * 1024;
    if (file.size > maxBytes) {
      return `حجم الملف كبير جدًا. الحد الأقصى ${this.maxSizeMb()} ميجابايت.`;
    }

    return null;
  }

  protected remove(event: Event): void {
    event.stopPropagation();
    if (this.disabled()) return;
    this.localPreviewUrl.set(null);
    this.fileName.set(null);
    this.cleared.emit();
  }

  /** Reverts any locally-selected (not-yet-saved) preview back to `previewUrl()`. */
  reset(): void {
    this.localPreviewUrl.set(null);
    this.fileName.set(null);
    this.previewLoadFailed.set(false);
  }
}
