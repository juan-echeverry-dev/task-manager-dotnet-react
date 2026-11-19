import type { RouteObject } from 'react-router'
import { RedirectIfAuthenticated } from '../features/auth/RedirectIfAuthenticated'
import { RegisterPage } from '../features/auth/RegisterPage'
import { RequireAuth } from '../features/auth/RequireAuth'
import { SignInPage } from '../features/auth/SignInPage'
import { ProfilePage } from '../features/profile/ProfilePage'
import { NewTaskPage } from '../features/tasks/NewTaskPage'
import { TaskListPage } from '../features/tasks/TaskListPage'
import { AppLayout } from '../shared/layout/AppLayout'
import { HomePage } from './routes/HomePage'
import { NotFoundPage } from './routes/NotFoundPage'

/** Route table. Feature routes are added here as each spec is implemented. */
export const routes: RouteObject[] = [
  {
    element: <AppLayout />,
    children: [
      { index: true, element: <HomePage /> },
      {
        element: <RedirectIfAuthenticated />,
        children: [
          { path: 'register', element: <RegisterPage /> },
          { path: 'sign-in', element: <SignInPage /> },
        ],
      },
      {
        element: <RequireAuth />,
        children: [
          { path: 'profile', element: <ProfilePage /> },
          { path: 'tasks', element: <TaskListPage /> },
          { path: 'tasks/new', element: <NewTaskPage /> },
        ],
      },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
]
