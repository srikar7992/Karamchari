'use client';

import React, { useEffect } from 'react';
import { useAuth } from './AuthContext';

interface ProtectedRouteProps {
  children: React.ReactNode;
  roles?: string[];
}

export const ProtectedRoute: React.FC<ProtectedRouteProps> = ({ children, roles }) => {
  const { isAuthenticated, isLoading, user } = useAuth();

  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      window.location.assign('/login');
    }
  }, [isAuthenticated, isLoading]);

  if (isLoading) {
    return <div>Loading authentication...</div>;
  }

  if (!isAuthenticated) {
    return null;
  }

  if (roles && roles.length > 0 && user && !roles.some((role) => user.roles.includes(role))) {
    window.location.assign('/unauthorized');
    return null;
  }

  return <>{children}</>;
};
