import { ChangeDetectionStrategy, Component, signal } from '@angular/core';

interface GenerationCard {
  id: number;
  kind: 'image' | 'video' | 'text-image';
  iconClass: string;
  label: string;
  statusLabel: string;
}

interface PlatformNode {
  id: number;
  key: string;
  iconClass: string;
  label: string;
  x: number;
}

@Component({
  selector: 'app-workflow-visual',
  imports: [],
  templateUrl: './workflow-visual.html',
  styleUrl: './workflow-visual.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WorkflowVisual {
  readonly inputCard = {
    iconClass: 'fa-solid fa-magnifying-glass',
    label: 'تحليل نشاطك',
    statusLabel: 'جاري التحليل...',
  };

  readonly generationCards = signal<GenerationCard[]>([
    {
      id: 1,
      kind: 'image',
      iconClass: 'fa-solid fa-image',
      label: 'إنشاء صورة',
      statusLabel: 'جاري التوليد...',
    },
    {
      id: 2,
      kind: 'video',
      iconClass: 'fa-solid fa-clapperboard',
      label: 'إنشاء فيديو',
      statusLabel: 'جاري الرندر...',
    },
    {
      id: 3,
      kind: 'text-image',
      iconClass: 'fa-solid fa-pen-nib',
      label: 'صورة + كابشن',
      statusLabel: 'جاري الكتابة...',
    },
  ]);

  readonly platforms = signal<PlatformNode[]>([
    { id: 1, key: 'instagram', iconClass: 'fa-brands fa-instagram', label: 'انستغرام', x: 6 },
    { id: 2, key: 'facebook', iconClass: 'fa-brands fa-facebook-f', label: 'فيسبوك', x: 27 },
    { id: 3, key: 'snapchat', iconClass: 'fa-brands fa-snapchat', label: 'سناب شات', x: 50 },
    { id: 4, key: 'tiktok', iconClass: 'fa-brands fa-tiktok', label: 'تيك توك', x: 73 },
    { id: 5, key: 'linkedin', iconClass: 'fa-brands fa-linkedin-in', label: 'لينكدإن', x: 94 },
  ]);
}
