import { Injectable, computed, signal } from '@angular/core';
import { ScheduledPost } from '../model/scheduled-post.model';

const INITIAL_POSTS: ScheduledPost[] = [
  // ─ June 1 ─
  { id: 'p1',  campaignId: '1', campaignName: 'حملة رمضان الكريم', platform: 'instagram', content: 'اكتشف تشكيلتنا الجديدة بمناسبة رمضان الكريم 🌙\nخصومات تصل إلى ٥٠٪ على جميع المنتجات المختارة', mediaType: 'image',    scheduledAt: '2026-06-01T10:00:00', status: 'published', hashtags: ['#رمضان_كريم', '#عروض', '#تخفيضات'],    estimatedReach: 45000 },
  { id: 'p2',  campaignId: '1', campaignName: 'حملة رمضان الكريم', platform: 'facebook',  content: 'عروض رمضان لا تفوتك! تسوق الآن واستمتع بأفضل الأسعار',                                                   mediaType: 'image',    scheduledAt: '2026-06-01T14:00:00', status: 'published', hashtags: ['#رمضان', '#عروض_مميزة'],                estimatedReach: 32000 },
  // ─ June 3 ─
  { id: 'p3b', campaignId: '3', campaignName: 'حملة الصيف',        platform: 'instagram', content: 'الصيف قادم! جهّز نفسك مع أفضل منتجاتنا الصيفية ☀️',                                                       mediaType: 'story',    scheduledAt: '2026-06-03T08:30:00', status: 'published', hashtags: ['#صيف', '#منتجات_صيفية'] },
  // ─ June 4 (today) ─
  { id: 'p3',  campaignId: '2', campaignName: 'إطلاق منتج العيد',  platform: 'instagram', content: 'منتجنا الجديد وصل أخيراً! كن أول من يجربه ويشارك تجربته معنا 🎉',                                          mediaType: 'carousel', scheduledAt: '2026-06-04T09:00:00', status: 'scheduled', hashtags: ['#منتج_جديد', '#عيد', '#إطلاق'],          estimatedReach: 58000 },
  { id: 'p4',  campaignId: '2', campaignName: 'إطلاق منتج العيد',  platform: 'tiktok',    content: 'شاهد كيف يعمل منتجنا الجديد في هذا الفيديو المميز 🎬 #ترند',                                               mediaType: 'video',    scheduledAt: '2026-06-04T12:00:00', status: 'scheduled', hashtags: ['#تيك_توك', '#منتج', '#ترند'],               estimatedReach: 120000 },
  { id: 'p5',  campaignId: '1', campaignName: 'حملة رمضان الكريم', platform: 'facebook',  content: 'لا تفوت عرض اليوم! خصم خاص لمتابعينا الكرام ٣٠٪ على الطلبات فوق ٢٠٠ ريال',                               mediaType: 'image',    scheduledAt: '2026-06-04T18:00:00', status: 'scheduled', hashtags: ['#خصم', '#عرض_خاص'],                        estimatedReach: 28000 },
  // ─ June 5 ─
  { id: 'p6',  campaignId: '1', campaignName: 'حملة رمضان الكريم', platform: 'instagram', content: 'استمر الزخم! شارك مع أصدقائك وحصل على خصم إضافي',                                                          mediaType: 'story',    scheduledAt: '2026-06-05T11:00:00', status: 'scheduled', hashtags: ['#شارك_واربح'],                             estimatedReach: 15000 },
  { id: 'p7',  campaignId: '3', campaignName: 'حملة الصيف',        platform: 'youtube',   content: 'فيديو جديد: نصائح الصيف الذهبية من خبرائنا لتكون مستعداً لهذا الصيف 🎥',                                   mediaType: 'video',    scheduledAt: '2026-06-05T16:00:00', status: 'scheduled', hashtags: ['#نصائح_صيفية', '#يوتيوب'],                 estimatedReach: 22000 },
  // ─ June 8 ─
  { id: 'p8',  campaignId: '2', campaignName: 'إطلاق منتج العيد',  platform: 'snapchat',  content: 'قصة سناب حصرية: كواليس إطلاق منتجنا الجديد 👻',                                                            mediaType: 'story',    scheduledAt: '2026-06-08T10:00:00', status: 'scheduled', hashtags: ['#سناب_شات', '#حصري'],                      estimatedReach: 18000 },
  { id: 'p8b', campaignId: '3', campaignName: 'حملة الصيف',        platform: 'facebook',  content: 'أبرز منتجات الصيف لهذا العام — قائمة مختارة بعناية',                                                       mediaType: 'image',    scheduledAt: '2026-06-08T15:00:00', status: 'scheduled',                                                    estimatedReach: 24000 },
  // ─ June 10 ─
  { id: 'p9',  campaignId: '1', campaignName: 'حملة رمضان الكريم', platform: 'instagram', content: 'منتصف الرحلة ونحن لا زلنا نقدم أفضل العروض! لا تفوتها',                                                   mediaType: 'carousel', scheduledAt: '2026-06-10T09:00:00', status: 'scheduled',                                                    estimatedReach: 41000 },
  { id: 'p10', campaignId: '3', campaignName: 'حملة الصيف',        platform: 'facebook',  content: 'تحضيرات الصيف: قائمة مشترياتك الأساسية التي لا يمكنك الاستغناء عنها',                                     mediaType: 'image',    scheduledAt: '2026-06-10T15:00:00', status: 'scheduled',                                                    estimatedReach: 19000 },
  // ─ June 12 ─
  { id: 'p11', campaignId: '2', campaignName: 'إطلاق منتج العيد',  platform: 'instagram', content: 'آراء عملائنا تتحدث عن نفسها! تجربة حقيقية من عملاء حقيقيين',                                              mediaType: 'image',    scheduledAt: '2026-06-12T11:00:00', status: 'scheduled', hashtags: ['#آراء_العملاء', '#تجربة_حقيقية'],           estimatedReach: 36000 },
  { id: 'p11b',campaignId: '1', campaignName: 'حملة رمضان الكريم', platform: 'x',         content: 'أسعار لم نقدمها من قبل! تفقد عروضنا الحصرية الآن 🔥',                                                      mediaType: 'image',    scheduledAt: '2026-06-12T14:00:00', status: 'scheduled', hashtags: ['#عروض_حصرية'],                             estimatedReach: 12000 },
  // ─ June 15 ─
  { id: 'p12', campaignId: '3', campaignName: 'حملة الصيف',        platform: 'tiktok',    content: 'تحدي الصيف ابدأ معنا الآن! 🌊☀️ شارك مقطعك وستظهر على صفحتنا',                                            mediaType: 'video',    scheduledAt: '2026-06-15T14:00:00', status: 'scheduled', hashtags: ['#تحدي_الصيف', '#تيك_توك'],                 estimatedReach: 95000 },
  { id: 'p13', campaignId: '1', campaignName: 'حملة رمضان الكريم', platform: 'linkedin',  content: 'شراكات تجارية جديدة وتوسع مستمر في السوق الخليجي — نتطلع للتعاون معكم',                                   mediaType: 'image',    scheduledAt: '2026-06-15T09:00:00', status: 'scheduled',                                                    estimatedReach: 8500  },
  // ─ June 18 ─
  { id: 'p18', campaignId: '2', campaignName: 'إطلاق منتج العيد',  platform: 'instagram', content: 'أسبوع التميز! خصومات إضافية تصل لـ ٤٠٪ على المجموعة الكاملة',                                             mediaType: 'carousel', scheduledAt: '2026-06-18T10:00:00', status: 'scheduled',                                                    estimatedReach: 52000 },
  // ─ June 20 ─
  { id: 'p14', campaignId: '3', campaignName: 'حملة الصيف',        platform: 'instagram', content: 'نصف الطريق للصيف الكامل! وعروضنا لا تتوقف، ابقوا معنا',                                                   mediaType: 'carousel', scheduledAt: '2026-06-20T10:00:00', status: 'scheduled',                                                    estimatedReach: 44000 },
  { id: 'p20b',campaignId: '1', campaignName: 'حملة رمضان الكريم', platform: 'facebook',  content: 'الأسبوع الأخير! سارع قبل انتهاء العروض التي قد لا تتكرر',                                                 mediaType: 'image',    scheduledAt: '2026-06-20T17:00:00', status: 'scheduled',                                                    estimatedReach: 31000 },
  // ─ June 22 ─
  { id: 'p22', campaignId: '3', campaignName: 'حملة الصيف',        platform: 'youtube',   content: 'حلقة جديدة: كيف تستعد لعطلة الصيف بأقل تكلفة وأعلى جودة',                                                 mediaType: 'video',    scheduledAt: '2026-06-22T14:00:00', status: 'scheduled',                                                    estimatedReach: 27000 },
  // ─ June 25 ─
  { id: 'p15', campaignId: '2', campaignName: 'إطلاق منتج العيد',  platform: 'youtube',   content: 'الحلقة الأخيرة: مراجعة شاملة لمنتجنا مع الخبراء 🎥',                                                      mediaType: 'video',    scheduledAt: '2026-06-25T16:00:00', status: 'scheduled',                                                    estimatedReach: 35000 },
  { id: 'p16', campaignId: '1', campaignName: 'حملة رمضان الكريم', platform: 'facebook',  content: 'آخر أيام العروض الاستثنائية! سارع قبل انتهاء المخزون للأبد',                                              mediaType: 'image',    scheduledAt: '2026-06-25T18:00:00', status: 'scheduled', hashtags: ['#آخر_فرصة', '#عروض'],                       estimatedReach: 29000 },
  // ─ June 28 ─
  { id: 'p28', campaignId: '3', campaignName: 'حملة الصيف',        platform: 'instagram', content: 'الختام الكبير لحملة الصيف! شكراً لكل من شارك وتفاعل معنا 🙏',                                             mediaType: 'image',    scheduledAt: '2026-06-28T10:00:00', status: 'scheduled',                                                    estimatedReach: 48000 },
  // ─ June 30 ─
  { id: 'p17', campaignId: '3', campaignName: 'حملة الصيف',        platform: 'instagram', content: 'نهاية شهر وبداية فصل جديد! شكراً لدعمكم المستمر، والمزيد قادم 🙏',                                        mediaType: 'image',    scheduledAt: '2026-06-30T10:00:00', status: 'scheduled', hashtags: ['#شكراً', '#معاً_للأمام'],                  estimatedReach: 51000 },
];

@Injectable({ providedIn: 'root' })
export class ScheduledPostService {
  private readonly _posts = signal<ScheduledPost[]>(INITIAL_POSTS);
  readonly posts = this._posts.asReadonly();

  getById(id: string) {
    return computed(() => this._posts().find(p => p.id === id));
  }

  byCampaign(campaignId: string) {
    return computed(() => this._posts().filter(p => p.campaignId === campaignId));
  }

  update(post: ScheduledPost): void {
    this._posts.update(list => list.map(p => (p.id === post.id ? post : p)));
  }

  remove(id: string): void {
    this._posts.update(list => list.filter(p => p.id !== id));
  }
}
