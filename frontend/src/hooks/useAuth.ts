import { useCallback, useEffect, useState } from 'react';
import { API_URL } from '../lib/api';

export type AuthStatus = 'loading' | 'authenticated' | 'unauthenticated';

function fetchSession(): Promise<{ username: string } | null> {
  return fetch(`${API_URL}/auth/me`, { credentials: 'include' })
    .then((res) => {
      if (!res.ok) throw new Error('unauthenticated');
      return res.json();
    })
    .catch(() => null);
}

export function useAuth() {
  const [status, setStatus] = useState<AuthStatus>('loading');
  const [username, setUsername] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetchSession().then((data) => {
      setUsername(data?.username ?? null);
      setStatus(data ? 'authenticated' : 'unauthenticated');
    });
  }, []);

  const login = useCallback(async (loginUsername: string, password: string) => {
    setError(null);

    const res = await fetch(`${API_URL}/auth/login`, {
      method: 'POST',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ username: loginUsername, password }),
    });

    if (!res.ok) {
      setError(res.status === 429 ? 'Too many attempts. Try again in a minute.' : 'Invalid username or password.');
      return false;
    }

    const data = await fetchSession();
    setUsername(data?.username ?? null);
    setStatus(data ? 'authenticated' : 'unauthenticated');
    return true;
  }, []);

  const logout = useCallback(async () => {
    await fetch(`${API_URL}/auth/logout`, { method: 'POST', credentials: 'include' });
    setUsername(null);
    setStatus('unauthenticated');
  }, []);

  return { status, username, error, login, logout };
}
