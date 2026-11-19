import type { RouteObject } from 'react-router'
import { RedirectIfAuthenticated } from '../features/auth/RedirectIfAuthenticated'
import { RegisterPage } from '../features/auth/RegisterPage'
import { RequireAuth } from '../features/auth/RequireAuth'
import { SignInPage } from '../features/auth/SignInPage'
import { ProfilePage } from '../features/profile/ProfilePage'
import { EditTaskPanel } from '../features/tasks/EditTaskPanel'
import { NewTaskPanel } from '../features/tasks/NewTaskPanel'
import { TaskBoardPage } from '../features/tasks/TaskBoardPage'
import { TaskDetailPanel } from '../features/tasks/TaskDetailPanel'
import { TaskPanel } from '../features/tasks/TaskPanel'
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
          {
            // The board stays mounted while the side panel opens and closes (spec 004, research R3).
            path: 'tasks',
            element: <TaskBoardPage />,
            children: [
              {
                element: <TaskPanel />,
                children: [
                  { path: 'new', element: <NewTaskPanel /> },
                  { path: ':id', element: <TaskDetailPanel /> },
                  { path: ':id/edit', element: <EditTaskPanel /> },
                ],
              },
            ],
          },
        ],
      },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
]
