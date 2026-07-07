import { Component, computed, effect, input, output, signal } from '@angular/core';
import { GeneratedItem, GenType, TYPE_CFG } from '../../../model/generated-item.model';
import {
  CalendarPost, EditedPlan, PlanData,
} from '../marketing-plan-page/marketing-plan-page';

const CAL_DAYS = ['السبت', 'الأحد', 'الاثنين', 'الثلاثاء', 'الأربعاء', 'الخميس', 'الجمعة'];
const MONTH_NAMES_AR = [
  'الأول', 'الثاني', 'الثالث', 'الرابع', 'الخامس', 'السادس',
  'السابع', 'الثامن', 'التاسع', 'العاشر', 'الحادي عشر', 'الثاني عشر',
];

@Component({
  selector: 'app-mp-plan-detail',
  standalone: true,
  imports: [],
  templateUrl: './mp-plan-detail.html',
  styleUrl: './mp-plan-detail.css',
})
export class MpPlanDetail {
  plan             = input.required<PlanData>();
  initialPosts     = input.required<CalendarPost[]>();
  savedPlansCount  = input<number>(0);

  remakeClicked  = output<void>();
  approveClicked = output<void>();
  backClicked    = output<void>();
  editSaved      = output<EditedPlan>();

  readonly typeCfg = TYPE_CFG;
  readonly calDays = CAL_DAYS;

  readonly allGoals = [
    'زيادة الوعي', 'التفاعل والمجتمع', 'رفع المبيعات',
    'توليد عملاء', 'إطلاق منتج', 'حركة المتجر',
  ];

  // ── internal state ──
  readonly isEditing     = signal(false);
  readonly localPosts    = signal<CalendarPost[]>([]);
  readonly calView       = signal<'month' | 'week'>('month');
  readonly calPage       = signal(0);
  readonly dragPostId    = signal<string | null>(null);
  readonly dropTargetKey = signal<string | null>(null);
  readonly selectedPost  = signal<CalendarPost | null>(null);
  readonly selectedMedia = signal<GeneratedItem | null>(null);

  readonly editBrand      = signal('');
  readonly editGoals      = signal<string[]>([]);
  readonly editBudgetFrom = signal(0);
  readonly editBudgetTo   = signal(0);

  // ── calendar computed ──
  readonly calMonthCount = computed(() => {
    const months = this.localPosts().reduce((max, p) => Math.max(max, p.monthIdx + 1), 1);
    return Math.max(1, months);
  });

  readonly calMaxPage = computed(() =>
    this.calView() === 'month'
      ? this.calMonthCount() - 1
      : this.calMonthCount() * 4 - 1
  );

  readonly calPageLabel = computed(() => {
    const page = this.calPage();
    if (this.calView() === 'month') return `الشهر ${MONTH_NAMES_AR[page] ?? page + 1}`;
    const mIdx = Math.floor(page / 4);
    const wNum = (page % 4) + 1;
    return `الأسبوع ${wNum} — الشهر ${MONTH_NAMES_AR[mIdx] ?? mIdx + 1}`;
  });

  readonly calendarGrid = computed(() => {
    const view     = this.calView();
    const page     = this.calPage();
    const posts    = this.localPosts();
    const dropKey  = this.dropTargetKey();
    const dragging = this.dragPostId() !== null;

    if (view === 'month') {
      const mp = posts.filter(p => p.monthIdx === page);
      return Array.from({ length: 4 }, (_, row) =>
        Array.from({ length: 7 }, (_, col) => ({
          dayNum: row * 7 + col + 1,
          key: `${row}-${col}`,
          isDrop: dragging && dropKey === `${row}-${col}`,
          posts: mp.filter(p => p.weekRow === row && p.dayCol === col),
        }))
      );
    } else {
      const mIdx    = Math.floor(page / 4);
      const weekRow = page % 4;
      const wp = posts.filter(p => p.monthIdx === mIdx && p.weekRow === weekRow);
      return [Array.from({ length: 7 }, (_, col) => ({
        dayNum: weekRow * 7 + col + 1,
        key: `0-${col}`,
        isDrop: dragging && dropKey === `0-${col}`,
        posts: wp.filter(p => p.dayCol === col),
      }))];
    }
  });

  constructor() {
    // sync local posts + reset state when parent changes the plan
    effect(() => {
      this.localPosts.set([...this.initialPosts()]);
      this.calPage.set(0);
      this.calView.set('month');
      this.isEditing.set(false);
      this.selectedPost.set(null);
      this.selectedMedia.set(null);
      this.dragPostId.set(null);
      this.dropTargetKey.set(null);
    });
  }

  // ── Calendar nav ──
  prevPage(): void { if (this.calPage() > 0) this.calPage.update(p => p - 1); }
  nextPage(): void { if (this.calPage() < this.calMaxPage()) this.calPage.update(p => p + 1); }
  switchView(v: 'month' | 'week'): void { this.calView.set(v); this.calPage.set(0); }

  // ── Drag & drop ──
  onDragStart(id: string): void { this.dragPostId.set(id); }
  onDragEnd(): void { this.dragPostId.set(null); this.dropTargetKey.set(null); }
  onDragOver(key: string, e: DragEvent): void { e.preventDefault(); this.dropTargetKey.set(key); }
  onDragLeave(key: string): void { if (this.dropTargetKey() === key) this.dropTargetKey.set(null); }
  onDrop(key: string, e: DragEvent): void {
    e.preventDefault();
    const postId = this.dragPostId();
    if (!postId) return;
    const [rowStr, colStr] = key.split('-');
    const col = +colStr;
    const view = this.calView();
    const page = this.calPage();
    const targetMonth = view === 'month' ? page : Math.floor(page / 4);
    const targetWeek  = view === 'month' ? +rowStr : page % 4;
    this.localPosts.update(ps => ps.map(p =>
      p.id === postId
        ? { ...p, monthIdx: targetMonth, weekRow: targetWeek, dayCol: col, day: CAL_DAYS[col] }
        : p
    ));
    this.dragPostId.set(null);
    this.dropTargetKey.set(null);
  }

  // ── Modals ──
  openPost(post: CalendarPost): void   { this.selectedPost.set(post); }
  closePost(): void                    { this.selectedPost.set(null); }
  openMedia(item: GeneratedItem): void { this.selectedMedia.set(item); }
  closeMedia(): void                   { this.selectedMedia.set(null); }

  linkedMedia(post: CalendarPost): GeneratedItem | undefined {
    if (!post.linkedMediaId) return undefined;
    return this.plan().mediaItems.find(i => i.id === post.linkedMediaId);
  }

  // ── Edit ──
  startEdit(): void {
    const p = this.plan();
    this.editBrand.set(p.brandName);
    this.editGoals.set([...p.goals]);
    this.editBudgetFrom.set(p.budgetFrom);
    this.editBudgetTo.set(p.budgetTo);
    this.isEditing.set(true);
  }

  cancelEdit(): void { this.isEditing.set(false); }

  saveEdit(): void {
    this.editSaved.emit({
      brand:      this.editBrand(),
      goals:      this.editGoals(),
      budgetFrom: this.editBudgetFrom(),
      budgetTo:   this.editBudgetTo(),
    });
    this.isEditing.set(false);
  }

  toggleEditGoal(goal: string): void {
    const cur = this.editGoals();
    this.editGoals.set(cur.includes(goal) ? cur.filter(g => g !== goal) : [...cur, goal]);
  }

  // ── Helpers ──
  isImage(type: GenType, thumbUrl?: string): boolean { return type === 'static-ad' && !!thumbUrl; }
  isVideo(type: GenType, vidUrl?: string): boolean   { return type === 'video' && !!vidUrl; }

  thumbnailGradient(type: GenType): string {
    return ({
      'static-ad': 'linear-gradient(135deg,#fce4ec,#fce4ec66)',
      'video':     'linear-gradient(135deg,#fff3e0,#fff3e066)',
      'text':      'linear-gradient(135deg,#ede9fe,#ede9fe66)',
    } as Record<GenType, string>)[type];
  }

  genderLabel(g: string): string {
    return ({ female: 'إناث', male: 'ذكور', all: 'الجنسين' } as Record<string, string>)[g] ?? g;
  }

  formatNum(n: number): string {
    if (n >= 1_000_000) return (n / 1_000_000).toFixed(1) + 'م';
    if (n >= 1_000)     return (n / 1_000).toFixed(1) + 'ك';
    return Math.round(n).toLocaleString('ar-SA');
  }
}
