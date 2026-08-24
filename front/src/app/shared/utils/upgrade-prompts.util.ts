import { TenantService } from '../../core/tenant/tenant.service';
import { ErrorModalService } from '../../services/error-modal.service';

/** True once the tenant has as many brand profiles as its plan (+ any purchased add-on slots)
 *  allows — the same rule `CreateBrandProfileCommandHandler` enforces server-side. */
export function isBrandLimitReached(tenantService: TenantService): boolean {
  return tenantService.brandProfileCount() >= tenantService.maxBrands() + tenantService.extraBrandsPurchased();
}

/** Shows the right upgrade path for a blocked brand-profile creation: a Business tenant on the
 *  Free plan raises its cap by becoming an Agency (a plan change, not an add-on purchase); an
 *  Agency already at its (plan + add-ons) cap needs a higher tier or another add-on slot, both
 *  bought from billing. */
export function promptBrandLimitUpgrade(tenantService: TenantService, errorModalService: ErrorModalService): void {
  if (tenantService.isAgency()) {
    errorModalService.show(
      'وصلت إلى الحد الأقصى لعدد العلامات التجارية في باقتك الحالية. رقِّ باقتك أو أضف علامات إضافية من صفحة الفوترة.',
      {
        variant: 'warning',
        title: 'يلزم ترقية الباقة',
        actionLabel: 'الذهاب للفوترة',
        actionLink: ['/dashboard/billing'],
      },
    );
    return;
  }

  errorModalService.show(
    'حسابك الحالي كصاحب علامة تجارية يسمح بملف تعريف واحد فقط. رقِّ حسابك إلى وكالة تسويق لإدارة أكثر من علامة.',
    {
      variant: 'warning',
      title: 'يلزم ترقية الحساب',
      actionLabel: 'ترقية إلى وكالة',
      actionLink: ['/upgrade-tenant'],
    },
  );
}
