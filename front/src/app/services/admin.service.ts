import { Injectable, computed, signal } from '@angular/core';
import { PlatformTenant, PlatformUser, SubscriptionPlan, TenantStatus } from '../model/admin.model';

const MOCK_TENANTS: PlatformTenant[] = [
  { id: 't1', name: 'وكالة رواج للتسويق الرقمي', kind: 'agency',   ownerName: 'هيثم أحمد',  ownerEmail: 'haitham@rawaj.com',   plan: 'الاحترافية', status: 'active',    usersCount: 4, brandProfilesCount: 2, createdAt: '2025-01-10' },
  { id: 't2', name: 'عطور الشرق',                  kind: 'business', ownerName: 'منى سالم',   ownerEmail: 'mona@sharq.com',      plan: 'الأساسية',    status: 'active',    usersCount: 1, brandProfilesCount: 1, createdAt: '2025-03-22' },
  { id: 't3', name: 'ستايل هاوس',                  kind: 'business', ownerName: 'خالد ناصر',  ownerEmail: 'khaled@stylehouse.com', plan: 'الأساسية',  status: 'trial',     usersCount: 1, brandProfilesCount: 1, createdAt: '2026-06-01' },
  { id: 't4', name: 'وكالة نمو الإبداعية',          kind: 'agency',   ownerName: 'سارة فهد',   ownerEmail: 'sara@numou.com',      plan: 'المؤسسات',    status: 'active',    usersCount: 9, brandProfilesCount: 6, createdAt: '2025-05-14' },
  { id: 't5', name: 'واتش ستور',                    kind: 'business', ownerName: 'عمر يوسف',   ownerEmail: 'omar@watchstore.com', plan: 'الأساسية',    status: 'suspended', usersCount: 1, brandProfilesCount: 1, createdAt: '2025-08-02' },
];

const MOCK_USERS: PlatformUser[] = [
  { id: 'u1', name: 'هيثم أحمد',   email: 'haitham@rawaj.com',   tenantName: 'وكالة رواج للتسويق الرقمي', tenantId: 't1', role: 'مالك',  status: 'active',  joinDate: '2025-01-10', lastActiveAt: 'منذ ساعة' },
  { id: 'u2', name: 'سارة الأحمد', email: 'sara@agency.com',     tenantName: 'وكالة رواج للتسويق الرقمي', tenantId: 't1', role: 'مدير',   status: 'active',  joinDate: '2025-01-12', lastActiveAt: 'منذ ساعتين' },
  { id: 'u3', name: 'منى سالم',    email: 'mona@sharq.com',      tenantName: 'عطور الشرق',                tenantId: 't2', role: 'مالك',   status: 'active',  joinDate: '2025-03-22', lastActiveAt: 'منذ يوم' },
  { id: 'u4', name: 'خالد ناصر',   email: 'khaled@stylehouse.com', tenantName: 'ستايل هاوس',               tenantId: 't3', role: 'مالك',   status: 'pending', joinDate: '2026-06-01', lastActiveAt: 'لم يسجّل الدخول بعد' },
  { id: 'u5', name: 'سارة فهد',    email: 'sara@numou.com',      tenantName: 'وكالة نمو الإبداعية',       tenantId: 't4', role: 'مالك',   status: 'active',  joinDate: '2025-05-14', lastActiveAt: 'منذ 10 دقائق' },
  { id: 'u6', name: 'عمر يوسف',    email: 'omar@watchstore.com', tenantName: 'واتش ستور',                 tenantId: 't5', role: 'مالك',   status: 'suspended', joinDate: '2025-08-02', lastActiveAt: 'منذ 3 أسابيع' },
];

const MOCK_PLANS: SubscriptionPlan[] = [
  { id: 'p1', name: 'الأساسية',    price: 15,  billingCycle: 'monthly', maxBrands: 1,  maxUsers: 1,  subscriberCount: 3 },
  { id: 'p2', name: 'الاحترافية',  price: 49,  billingCycle: 'monthly', maxBrands: 3,  maxUsers: 5,  subscriberCount: 1 },
  { id: 'p3', name: 'المؤسسات',    price: 149, billingCycle: 'monthly', maxBrands: 20, maxUsers: 50, subscriberCount: 1 },
];

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly _tenants = signal<PlatformTenant[]>(MOCK_TENANTS);
  private readonly _users = signal<PlatformUser[]>(MOCK_USERS);
  private readonly _plans = signal<SubscriptionPlan[]>(MOCK_PLANS);

  readonly tenants = this._tenants.asReadonly();
  readonly users = this._users.asReadonly();
  readonly plans = this._plans.asReadonly();

  readonly stats = computed(() => {
    const tenants = this._tenants();
    const users = this._users();
    const plans = this._plans();
    const monthlyRevenue = tenants
      .filter(t => t.status !== 'suspended')
      .reduce((sum, t) => sum + (plans.find(p => p.name === t.plan)?.price ?? 0), 0);

    return {
      totalTenants: tenants.length,
      activeTenants: tenants.filter(t => t.status === 'active').length,
      trialTenants: tenants.filter(t => t.status === 'trial').length,
      totalUsers: users.length,
      monthlyRevenue,
    };
  });

  getTenantById(id: string) {
    return computed(() => this._tenants().find(t => t.id === id));
  }

  setTenantStatus(id: string, status: TenantStatus): void {
    this._tenants.update(list => list.map(t => (t.id === id ? { ...t, status } : t)));
  }

  suspendUser(id: string): void {
    this._users.update(list => list.map(u => (u.id === id ? { ...u, status: 'suspended' } : u)));
  }

  reactivateUser(id: string): void {
    this._users.update(list => list.map(u => (u.id === id ? { ...u, status: 'active' } : u)));
  }
}
