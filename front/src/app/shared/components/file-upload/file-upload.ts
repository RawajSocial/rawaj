import { ChangeDetectionStrategy, Component, input, output, signal } from '@angular/core';

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
  /** Externally-controlled preview (e.g. restored from a saved profile). */
  readonly previewUrl = input<string | null>(null);

  readonly fileSelected = output<File>();
  readonly cleared = output<void>();

  protected readonly localPreviewUrl = signal<string | null>(null);
  protected readonly fileName = signal<string | null>(null);

  protected get resolvedPreview(): string | null {
    return this.localPreviewUrl() ?? this.previewUrl();
  }

  protected onFileChange(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;

    this.fileName.set(file.name);
    const reader = new FileReader();
    reader.onload = e => this.localPreviewUrl.set(e.target?.result as string);
    reader.readAsDataURL(file);

    this.fileSelected.emit(file);
  }

  protected remove(): void {
    this.localPreviewUrl.set(null);
    this.fileName.set(null);
    this.cleared.emit();
  }
}
