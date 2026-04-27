import React from 'react';
import ReactDOM from 'react-dom/client';
import { createBrowserRouter, RouterProvider } from 'react-router-dom';
import './index.css';
import './styles/layout.css';
import './styles/components.css';
import './styles/views.css';
import App from './App';
import LoginPage from './views/LoginPage';
import RegisterPage from './views/RegisterPage';
import Dashboard from './views/Dashboard';
import BoardView from './views/BoardView';
import ProjectView from './views/ProjectView';
import ProfilePage from './views/ProfilePage';
import ProtectedRoute from './components/ProtectedRoute';
import { TopbarProvider } from './context/TopbarContext';

const router = createBrowserRouter([
    {
        path: '/',
        element: <TopbarProvider><App /></TopbarProvider>,
        children: [
            { path: 'login', element: <LoginPage /> },
            { path: 'register', element: <RegisterPage /> },
            {
                path: 'dashboard',
                element: <ProtectedRoute><Dashboard /></ProtectedRoute>
            },
            {
                path: 'projects/:projectId',
                element: <ProtectedRoute><ProjectView /></ProtectedRoute>
            },
            {
                path: 'board/:boardId',
                element: <ProtectedRoute><BoardView /></ProtectedRoute>
            },
            {
                path: 'profile',
                element: <ProtectedRoute><ProfilePage /></ProtectedRoute>
            },
            { index: true, element: <LoginPage /> },
        ]
    }
]);

ReactDOM.createRoot(document.getElementById('root')).render(
    <React.StrictMode>
        <RouterProvider router={router} />
    </React.StrictMode>
);