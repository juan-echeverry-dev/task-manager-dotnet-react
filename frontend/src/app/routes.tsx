import type { RouteObject } from 'react-router'
import { AppLayout } from '../shared/layout/AppLayout'
import { HomePage } from './routes/HomePage'
import { NotFoundPage } from './routes/NotFoundPage'

/** Route table. Feature routes are added here as each spec is implemented. */
export const routes: RouteObject[] = [
  {
    element: <AppLayout />,
    children: [
      { index: true, element: <HomePage /> },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
]
