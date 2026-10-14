// API Client for Operix Backend API

const BASE_URL = '/api';

export const getAuthToken = () => localStorage.getItem('operix_token');
export const setAuthToken = (token) => {
  if (token) localStorage.setItem('operix_token', token);
  else localStorage.removeItem('operix_token');
};

export const getSavedUser = () => {
  const user = localStorage.getItem('operix_user');
  return user ? JSON.parse(user) : null;
};
export const setSavedUser = (user) => {
  if (user) localStorage.setItem('operix_user', JSON.stringify(user));
  else localStorage.removeItem('operix_user');
};

async function request(endpoint, options = {}) {
  const token = getAuthToken();
  const headers = {
    'Content-Type': 'application/json',
    ...(options.headers || {}),
  };

  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  const response = await fetch(`${BASE_URL}${endpoint}`, {
    ...options,
    headers,
  });

  if (response.status === 204) {
    return null;
  }

  let data = null;
  const contentType = response.headers.get('content-type');
  if (contentType && contentType.includes('application/json')) {
    data = await response.json();
  } else {
    data = await response.text();
  }

  if (!response.ok) {
    const errorMsg = data?.detail || data?.title || (typeof data === 'string' ? data : 'An unexpected error occurred');
    const error = new Error(errorMsg);
    error.status = response.status;
    error.details = data;
    throw error;
  }

  return data;
}

export const api = {
  // System Health
  health: () => request('/health'),

  // Auth
  auth: {
    login: (identifier, password) =>
      request('/auth/login', {
        method: 'POST',
        body: JSON.stringify({
          email: identifier,
          usernameOrEmail: identifier,
          password,
        }),
      }),
    register: (userData) =>
      request('/auth/register', {
        method: 'POST',
        body: JSON.stringify(userData),
      }),
    me: () => request('/auth/me'),
  },

  // Dashboard
  dashboard: {
    summary: () => request('/dashboard/summary'),
    my: () => request('/dashboard/my'),
  },

  // Departments
  departments: {
    list: () => request('/departments'),
    create: (dept) =>
      request('/departments', {
        method: 'POST',
        body: JSON.stringify(dept),
      }),
    update: (id, dept) =>
      request(`/departments/${id}`, {
        method: 'PUT',
        body: JSON.stringify(dept),
      }),
  },

  // Employees
  employees: {
    list: (params = {}) => {
      const searchParams = new URLSearchParams();
      if (params.pageNumber) searchParams.set('pageNumber', params.pageNumber);
      if (params.pageSize) searchParams.set('pageSize', params.pageSize);
      if (params.search) searchParams.set('search', params.search);
      if (params.departmentId) searchParams.set('departmentId', params.departmentId);
      const query = searchParams.toString();
      return request(`/employees${query ? `?${query}` : ''}`);
    },
    get: (id) => request(`/employees/${id}`),
    create: (emp) =>
      request('/employees', {
        method: 'POST',
        body: JSON.stringify(emp),
      }),
    update: (id, emp) =>
      request(`/employees/${id}`, {
        method: 'PUT',
        body: JSON.stringify(emp),
      }),
  },

  // Leave Requests
  leave: {
    my: () => request('/leaverequests/my'),
    list: (params = {}) => {
      const searchParams = new URLSearchParams();
      if (params.pageNumber) searchParams.set('pageNumber', params.pageNumber);
      if (params.pageSize) searchParams.set('pageSize', params.pageSize);
      if (params.status) searchParams.set('status', params.status);
      if (params.departmentId) searchParams.set('departmentId', params.departmentId);
      const query = searchParams.toString();
      return request(`/leaverequests${query ? `?${query}` : ''}`);
    },
    submit: (leave) =>
      request('/leaverequests', {
        method: 'POST',
        body: JSON.stringify(leave),
      }),
    approve: (id) =>
      request(`/leaverequests/${id}/approve`, {
        method: 'POST',
      }),
    reject: (id, reason) =>
      request(`/leaverequests/${id}/reject`, {
        method: 'POST',
        body: JSON.stringify({ rejectionReason: reason }),
      }),
    cancel: (id) =>
      request(`/leaverequests/${id}/cancel`, {
        method: 'POST',
      }),
  },

  // Tasks
  tasks: {
    list: (params = {}) => {
      const searchParams = new URLSearchParams();
      if (params.pageNumber) searchParams.set('pageNumber', params.pageNumber);
      if (params.pageSize) searchParams.set('pageSize', params.pageSize);
      if (params.status) searchParams.set('status', params.status);
      if (params.priority) searchParams.set('priority', params.priority);
      if (params.assigneeId) searchParams.set('assigneeId', params.assigneeId);
      if (params.search) searchParams.set('search', params.search);
      const query = searchParams.toString();
      return request(`/tasks${query ? `?${query}` : ''}`);
    },
    create: (task) =>
      request('/tasks', {
        method: 'POST',
        body: JSON.stringify(task),
      }),
    update: (id, task) =>
      request(`/tasks/${id}`, {
        method: 'PUT',
        body: JSON.stringify(task),
      }),
    start: (id) => request(`/tasks/${id}/start`, { method: 'POST' }),
    review: (id) => request(`/tasks/${id}/review`, { method: 'POST' }),
    requestChanges: (id) => request(`/tasks/${id}/request-changes`, { method: 'POST' }),
    complete: (id) => request(`/tasks/${id}/complete`, { method: 'POST' }),
    cancel: (id) => request(`/tasks/${id}/cancel`, { method: 'POST' }),
  },

  // Expenses
  expenses: {
    my: () => request('/expenses/my'),
    list: (params = {}) => {
      const searchParams = new URLSearchParams();
      if (params.pageNumber) searchParams.set('pageNumber', params.pageNumber);
      if (params.pageSize) searchParams.set('pageSize', params.pageSize);
      if (params.status) searchParams.set('status', params.status);
      if (params.category) searchParams.set('category', params.category);
      if (params.departmentId) searchParams.set('departmentId', params.departmentId);
      const query = searchParams.toString();
      return request(`/expenses${query ? `?${query}` : ''}`);
    },
    submit: (expense) =>
      request('/expenses', {
        method: 'POST',
        body: JSON.stringify(expense),
      }),
    approve: (id) =>
      request(`/expenses/${id}/approve`, {
        method: 'POST',
      }),
    reject: (id, reason) =>
      request(`/expenses/${id}/reject`, {
        method: 'POST',
        body: JSON.stringify({ rejectionReason: reason }),
      }),
    cancel: (id) =>
      request(`/expenses/${id}/cancel`, {
        method: 'POST',
      }),
  },
};
