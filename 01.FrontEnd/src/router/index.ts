import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/login',
      name: 'Login',
      component: () => import('@/views/auth/LoginView.vue'),
      meta: { requiresAuth: false },
    },
    {
      path: '/',
      component: () => import('@/layouts/MainLayout.vue'),
      meta: { requiresAuth: true },
      children: [
        { path: '', redirect: '/portal' },
        { path: 'portal', name: 'Portal', component: () => import('@/views/portal/PortalView.vue') },
        { path: 'portal/settings', name: 'PortalSettings', component: () => import('@/views/portal/PortalWebpartSettingsView.vue') },
        { path: 'approval', name: 'ApprovalInbox', component: () => import('@/views/approval/ApprovalInboxView.vue') },
        { path: 'approval/draft/:id?', name: 'ApprovalDraft', component: () => import('@/views/approval/ApprovalDraftView.vue') },
        { path: 'board', name: 'BoardList', component: () => import('@/views/board/BoardListView.vue') },
        { path: 'board/:boardId', name: 'PostList', component: () => import('@/views/board/PostListView.vue') },
        { path: 'board/:boardId/post/:postId', name: 'PostDetail', component: () => import('@/views/board/PostDetailView.vue') },
        { path: 'board/:boardId/write', name: 'PostWrite', component: () => import('@/views/board/PostWriteView.vue') },
        { path: 'approval/detail/:id', name: 'ApprovalDetail', component: () => import('@/views/approval/ApprovalDetailView.vue') },
        { path: 'calendar', name: 'Calendar', component: () => import('@/views/calendar/CalendarView.vue') },
        { path: 'calendar/resources', name: 'ResourceReservation', component: () => import('@/views/calendar/ResourceReservationView.vue') },
        { path: 'attendance', name: 'Attendance', component: () => import('@/views/attendance/AttendanceView.vue') },
        { path: 'organizations', name: 'Organizations', component: () => import('@/views/org/OrganizationView.vue') },
        { path: 'notifications', name: 'Notifications', component: () => import('@/views/notification/NotificationView.vue') },
        { path: 'admin', name: 'Admin', component: () => import('@/views/admin/AdminView.vue'), meta: { requiresAuth: true, roles: ['SystemAdmin', 'GroupwareAdmin'] } },
      ],
    },
    { path: '/:pathMatch(.*)*', redirect: '/' },
  ],
})

router.beforeEach((to, _from, next) => {
  const auth = useAuthStore()
  auth.loadFromStorage()

  if (to.meta.requiresAuth !== false && !auth.isAuthenticated) {
    next('/login')
  } else if (to.path === '/login' && auth.isAuthenticated) {
    next('/')
  } else {
    next()
  }
})

export default router
