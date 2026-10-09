import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import LoginPage from '../../pages/LoginPage';
import { useAuthStore } from '../../store/authStore';

// Mock the auth store
vi.mock('../../store/authStore', () => ({
  useAuthStore: vi.fn(),
}));

// Mock react-router-dom navigate
const mockNavigate = vi.fn();
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

describe('LoginPage', () => {
  const mockLogin = vi.fn();
  const mockClearError = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
    (useAuthStore as unknown as ReturnType<typeof vi.fn>).mockReturnValue({
      login: mockLogin,
      isLoading: false,
      error: null,
      clearError: mockClearError,
    });
    // Mock getState for post-login check
    (useAuthStore as any).getState = vi.fn().mockReturnValue({
      isAuthenticated: false,
    });
  });

  // ── RENDERING ─────────────────────────────────────────────────────

  it('should render the login form with email and password fields', () => {
    render(
      <MemoryRouter>
        <LoginPage />
      </MemoryRouter>
    );

    expect(screen.getByLabelText(/email address/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/password/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /sign in/i })).toBeInTheDocument();
  });

  it('should render the TalentFlow AI branding', () => {
    render(
      <MemoryRouter>
        <LoginPage />
      </MemoryRouter>
    );

    expect(screen.getByText('TalentFlow')).toBeInTheDocument();
    expect(screen.getByText('AI')).toBeInTheDocument();
    expect(screen.getByText(/intelligent recruitment platform/i)).toBeInTheDocument();
  });

  it('should display "Welcome back" heading', () => {
    render(
      <MemoryRouter>
        <LoginPage />
      </MemoryRouter>
    );

    expect(screen.getByText(/welcome back/i)).toBeInTheDocument();
  });

  // ── FORM VALIDATION ───────────────────────────────────────────────

  it('should have required attribute on email input', () => {
    render(
      <MemoryRouter>
        <LoginPage />
      </MemoryRouter>
    );

    const emailInput = screen.getByLabelText(/email address/i);
    expect(emailInput).toBeRequired();
  });

  it('should have required attribute on password input', () => {
    render(
      <MemoryRouter>
        <LoginPage />
      </MemoryRouter>
    );

    const passwordInput = screen.getByLabelText(/password/i);
    expect(passwordInput).toBeRequired();
  });

  it('should have email type on email input', () => {
    render(
      <MemoryRouter>
        <LoginPage />
      </MemoryRouter>
    );

    const emailInput = screen.getByLabelText(/email address/i);
    expect(emailInput).toHaveAttribute('type', 'email');
  });

  it('should have password type on password input', () => {
    render(
      <MemoryRouter>
        <LoginPage />
      </MemoryRouter>
    );

    const passwordInput = screen.getByLabelText(/password/i);
    expect(passwordInput).toHaveAttribute('type', 'password');
  });

  // ── USER INTERACTION ──────────────────────────────────────────────

  it('should update email field when user types', async () => {
    render(
      <MemoryRouter>
        <LoginPage />
      </MemoryRouter>
    );

    const emailInput = screen.getByLabelText(/email address/i);
    await userEvent.type(emailInput, 'admin@talentflow.com');
    expect(emailInput).toHaveValue('admin@talentflow.com');
  });

  it('should update password field when user types', async () => {
    render(
      <MemoryRouter>
        <LoginPage />
      </MemoryRouter>
    );

    const passwordInput = screen.getByLabelText(/password/i);
    await userEvent.type(passwordInput, 'securePass123');
    expect(passwordInput).toHaveValue('securePass123');
  });

  it('should call login function on form submit', async () => {
    render(
      <MemoryRouter>
        <LoginPage />
      </MemoryRouter>
    );

    await userEvent.type(screen.getByLabelText(/email address/i), 'admin@talentflow.com');
    await userEvent.type(screen.getByLabelText(/password/i), 'password123');
    await userEvent.click(screen.getByRole('button', { name: /sign in/i }));

    expect(mockLogin).toHaveBeenCalledWith('admin@talentflow.com', 'password123');
  });

  it('should clear error before login attempt', async () => {
    render(
      <MemoryRouter>
        <LoginPage />
      </MemoryRouter>
    );

    await userEvent.type(screen.getByLabelText(/email address/i), 'test@test.com');
    await userEvent.type(screen.getByLabelText(/password/i), 'pass');
    await userEvent.click(screen.getByRole('button', { name: /sign in/i }));

    expect(mockClearError).toHaveBeenCalled();
  });

  // ── ERROR STATE ───────────────────────────────────────────────────

  it('should display error message when error exists', () => {
    (useAuthStore as unknown as ReturnType<typeof vi.fn>).mockReturnValue({
      login: mockLogin,
      isLoading: false,
      error: 'Invalid email or password',
      clearError: mockClearError,
    });

    render(
      <MemoryRouter>
        <LoginPage />
      </MemoryRouter>
    );

    expect(screen.getByText('Invalid email or password')).toBeInTheDocument();
  });

  it('should not display error message when no error', () => {
    render(
      <MemoryRouter>
        <LoginPage />
      </MemoryRouter>
    );

    expect(screen.queryByText(/invalid/i)).not.toBeInTheDocument();
  });

  // ── LOADING STATE ─────────────────────────────────────────────────

  it('should show loading indicator when isLoading is true', () => {
    (useAuthStore as unknown as ReturnType<typeof vi.fn>).mockReturnValue({
      login: mockLogin,
      isLoading: true,
      error: null,
      clearError: mockClearError,
    });

    render(
      <MemoryRouter>
        <LoginPage />
      </MemoryRouter>
    );

    expect(screen.getByText(/signing in/i)).toBeInTheDocument();
  });

  it('should disable submit button when loading', () => {
    (useAuthStore as unknown as ReturnType<typeof vi.fn>).mockReturnValue({
      login: mockLogin,
      isLoading: true,
      error: null,
      clearError: mockClearError,
    });

    render(
      <MemoryRouter>
        <LoginPage />
      </MemoryRouter>
    );

    const submitButton = screen.getByRole('button');
    expect(submitButton).toBeDisabled();
  });

  // ── NAVIGATION ────────────────────────────────────────────────────

  it('should navigate to home on successful login', async () => {
    (useAuthStore as any).getState = vi.fn().mockReturnValue({
      isAuthenticated: true,
    });

    render(
      <MemoryRouter>
        <LoginPage />
      </MemoryRouter>
    );

    await userEvent.type(screen.getByLabelText(/email address/i), 'admin@talentflow.com');
    await userEvent.type(screen.getByLabelText(/password/i), 'password123');
    await userEvent.click(screen.getByRole('button', { name: /sign in/i }));

    expect(mockNavigate).toHaveBeenCalledWith('/');
  });

  it('should not navigate when login fails', async () => {
    (useAuthStore as any).getState = vi.fn().mockReturnValue({
      isAuthenticated: false,
    });

    render(
      <MemoryRouter>
        <LoginPage />
      </MemoryRouter>
    );

    await userEvent.type(screen.getByLabelText(/email address/i), 'wrong@email.com');
    await userEvent.type(screen.getByLabelText(/password/i), 'wrongpass');
    await userEvent.click(screen.getByRole('button', { name: /sign in/i }));

    expect(mockNavigate).not.toHaveBeenCalled();
  });
});
