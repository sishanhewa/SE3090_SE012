import { describe, it, expect, beforeEach, vi } from 'vitest';
import { useAuthStore } from '../../store/authStore';

// Mock apiClient
vi.mock('../../api/apiClient', () => ({
  default: {
    post: vi.fn(),
  },
}));

import apiClient from '../../api/apiClient';

describe('authStore', () => {
  beforeEach(() => {
    // Reset the store state before each test
    useAuthStore.setState({
      accessToken: null,
      refreshToken: null,
      user: null,
      isAuthenticated: false,
      isLoading: false,
      error: null,
    });
    vi.clearAllMocks();
  });

  // ── INITIAL STATE ─────────────────────────────────────────────────

  it('should have correct initial state', () => {
    const state = useAuthStore.getState();

    expect(state.accessToken).toBeNull();
    expect(state.refreshToken).toBeNull();
    expect(state.user).toBeNull();
    expect(state.isAuthenticated).toBe(false);
    expect(state.isLoading).toBe(false);
    expect(state.error).toBeNull();
  });

  // ── LOGIN ─────────────────────────────────────────────────────────

  it('should set loading state during login', async () => {
    // Arrange – make apiClient.post return a pending promise
    let resolveLogin: (value: any) => void;
    (apiClient.post as ReturnType<typeof vi.fn>).mockReturnValue(
      new Promise((resolve) => { resolveLogin = resolve; })
    );

    // Act – start login (don't await)
    const loginPromise = useAuthStore.getState().login('test@test.com', 'password');

    // Assert – should be loading
    expect(useAuthStore.getState().isLoading).toBe(true);
    expect(useAuthStore.getState().error).toBeNull();

    // Cleanup
    resolveLogin!({ data: { accessToken: 'tok', refreshToken: 'ref', user: {} } });
    await loginPromise;
  });

  it('should set auth data on successful login', async () => {
    const mockUser = {
      id: '123',
      email: 'admin@talentflow.com',
      firstName: 'Admin',
      lastName: 'User',
      roles: ['Admin'],
      companyId: 'comp-1',
    };

    (apiClient.post as ReturnType<typeof vi.fn>).mockResolvedValue({
      data: {
        accessToken: 'mock-access-token',
        refreshToken: 'mock-refresh-token',
        user: mockUser,
      },
    });

    await useAuthStore.getState().login('admin@talentflow.com', 'password123');

    const state = useAuthStore.getState();
    expect(state.accessToken).toBe('mock-access-token');
    expect(state.refreshToken).toBe('mock-refresh-token');
    expect(state.user).toEqual(mockUser);
    expect(state.isAuthenticated).toBe(true);
    expect(state.isLoading).toBe(false);
    expect(state.error).toBeNull();
  });

  it('should set error on failed login', async () => {
    (apiClient.post as ReturnType<typeof vi.fn>).mockRejectedValue({
      response: {
        data: {
          message: 'Invalid email or password',
        },
      },
    });

    await useAuthStore.getState().login('wrong@email.com', 'wrongpass');

    const state = useAuthStore.getState();
    expect(state.error).toBe('Invalid email or password');
    expect(state.isAuthenticated).toBe(false);
    expect(state.isLoading).toBe(false);
    expect(state.accessToken).toBeNull();
  });

  it('should set default error message when no server message', async () => {
    (apiClient.post as ReturnType<typeof vi.fn>).mockRejectedValue(new Error('Network error'));

    await useAuthStore.getState().login('test@test.com', 'password');

    const state = useAuthStore.getState();
    expect(state.error).toBe('Login failed. Please check your credentials.');
    expect(state.isLoading).toBe(false);
  });

  // ── LOGOUT ────────────────────────────────────────────────────────

  it('should clear all auth data on logout', () => {
    // Arrange – set authenticated state
    useAuthStore.setState({
      accessToken: 'some-token',
      refreshToken: 'some-refresh',
      user: {
        id: '123',
        email: 'test@test.com',
        firstName: 'Test',
        lastName: 'User',
        roles: ['Admin'],
      },
      isAuthenticated: true,
    });

    // Act
    useAuthStore.getState().logout();

    // Assert
    const state = useAuthStore.getState();
    expect(state.accessToken).toBeNull();
    expect(state.refreshToken).toBeNull();
    expect(state.user).toBeNull();
    expect(state.isAuthenticated).toBe(false);
    expect(state.error).toBeNull();
  });

  // ── CLEAR ERROR ───────────────────────────────────────────────────

  it('should clear error when clearError is called', () => {
    useAuthStore.setState({ error: 'Some error' });

    useAuthStore.getState().clearError();

    expect(useAuthStore.getState().error).toBeNull();
  });

  // ── API CALL VERIFICATION ─────────────────────────────────────────

  it('should call the correct API endpoint on login', async () => {
    (apiClient.post as ReturnType<typeof vi.fn>).mockResolvedValue({
      data: { accessToken: 'tok', refreshToken: 'ref', user: {} },
    });

    await useAuthStore.getState().login('user@example.com', 'mypassword');

    expect(apiClient.post).toHaveBeenCalledWith('/Auth/login', {
      email: 'user@example.com',
      password: 'mypassword',
    });
  });
});
