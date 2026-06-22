'use client';

import React, { createContext, useContext, useState, useEffect } from 'react';

interface User {
  id: string;
  name: string;
  email: string;
  roles: string[];
  tenantId: string;
}

interface AuthContextType {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (token: string) => void;
  logout: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    // Check for token in localStorage on mount
    const token = localStorage.getItem('karamchari_token');
    if (token) {
      // Phase 1B: Mock user extraction from JWT
      // In production, validate token and extract claims
      setUser({
        id: '00000000-0000-0000-0000-000000000001',
        name: 'Dev User',
        email: 'dev@karamchari.com',
        roles: ['Admin'],
        tenantId: 'tenant_oakridge'
      });
    }
    setIsLoading(false);
  }, []);

  const login = (token: string) => {
    localStorage.setItem('karamchari_token', token);
    // Extract user from token and set state
    setUser({
      id: '00000000-0000-0000-0000-000000000001',
      name: 'Dev User',
      email: 'dev@karamchari.com',
      roles: ['Admin'],
      tenantId: 'tenant_oakridge'
    });
  };

  const logout = () => {
    localStorage.removeItem('karamchari_token');
    setUser(null);
  };

  return (
    <AuthContext.Provider value={{ user, isAuthenticated: !!user, isLoading, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
