import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import App from './App';
import { AuthProvider } from './auth';
import { RequireAuth } from './ui';
import { HomePage } from './pages/HomePage';
import { SearchPage } from './pages/SearchPage';
import { RoomPage } from './pages/RoomPage';
import { LoginPage } from './pages/LoginPage';
import { RegisterPage } from './pages/RegisterPage';
import { ForgotPasswordPage } from './pages/ForgotPasswordPage';
import { ResetPasswordPage } from './pages/ResetPasswordPage';
import { PrivacyPage } from './pages/PrivacyPage';
import { ProfilePage } from './pages/ProfilePage';
import { ChangePasswordPage } from './pages/ChangePasswordPage';
import { BookingsPage } from './pages/BookingsPage';
import { PayPage } from './pages/PayPage';
import { FrontDeskPage } from './pages/FrontDeskPage';
import { AdminRoomsPage } from './pages/AdminRoomsPage';
import { RoomFormPage } from './pages/RoomFormPage';
import { RoomTypesPage } from './pages/RoomTypesPage';
import { CustomersPage } from './pages/CustomersPage';
import { ReportsPage } from './pages/ReportsPage';
import './styles.css';

createRoot(document.getElementById('root')).render(
  <StrictMode>
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route element={<App />}>
            <Route index element={<HomePage />} />
            <Route path="search" element={<SearchPage />} />
            <Route path="rooms/:id" element={<RoomPage />} />
            <Route path="login" element={<LoginPage />} />
            <Route path="register" element={<RegisterPage />} />
            <Route path="forgot" element={<ForgotPasswordPage />} />
            <Route path="reset" element={<ResetPasswordPage />} />
            <Route path="privacy" element={<PrivacyPage />} />
            <Route path="profile" element={<RequireAuth><ProfilePage /></RequireAuth>} />
            <Route path="password" element={<RequireAuth><ChangePasswordPage /></RequireAuth>} />
            <Route path="bookings" element={<RequireAuth><BookingsPage /></RequireAuth>} />
            <Route path="pay/:id" element={<RequireAuth><PayPage /></RequireAuth>} />
            <Route path="frontdesk" element={<RequireAuth roles={['Receptionist', 'Manager', 'Admin']}><FrontDeskPage /></RequireAuth>} />
            <Route path="admin/rooms" element={<RequireAuth roles={['Manager', 'Admin']}><AdminRoomsPage /></RequireAuth>} />
            <Route path="admin/rooms/new" element={<RequireAuth roles={['Manager', 'Admin']}><RoomFormPage /></RequireAuth>} />
            <Route path="admin/rooms/:id" element={<RequireAuth roles={['Manager', 'Admin']}><RoomFormPage /></RequireAuth>} />
            <Route path="admin/types" element={<RequireAuth roles={['Manager', 'Admin']}><RoomTypesPage /></RequireAuth>} />
            <Route path="admin/customers" element={<RequireAuth roles={['Manager', 'Admin']}><CustomersPage /></RequireAuth>} />
            <Route path="reports" element={<RequireAuth roles={['Manager', 'Admin']}><ReportsPage /></RequireAuth>} />
            <Route path="*" element={<Navigate to="/" replace />} />
          </Route>
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  </StrictMode>
);
