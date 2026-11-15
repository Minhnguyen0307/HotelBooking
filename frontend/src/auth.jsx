import { createContext, useContext, useEffect, useState } from 'react';
import { api } from './api';

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const [user, setUser] = useState(null);
  const [ready, setReady] = useState(false);

  useEffect(() => {
    const token = localStorage.getItem('token');
    if (!token) {
      setReady(true);
      return;
    }

    api('/api/auth/me')
      .then(setUser)
      .catch(() => {
        localStorage.removeItem('token');
        setUser(null);
      })
      .finally(() => setReady(true));
  }, []);

  function saveSession(result) {
    localStorage.setItem('token', result.token);
    setUser(result.user);
    return result.user;
  }

  async function login(email, password) {
    const result = await api('/api/auth/login', {
      method: 'POST',
      body: { email, password }
    });
    return saveSession(result);
  }

  async function loginWithGoogle(idToken) {
    const result = await api('/api/auth/google', {
      method: 'POST',
      body: { idToken }
    });
    return saveSession(result);
  }

  function logout() {
    localStorage.removeItem('token');
    setUser(null);
  }

  function hasRole(...roles) {
    return Boolean(user && roles.includes(user.role));
  }

  return (
    <AuthContext.Provider value={{ user, setUser, ready, login, loginWithGoogle, logout, hasRole }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  return useContext(AuthContext);
}
