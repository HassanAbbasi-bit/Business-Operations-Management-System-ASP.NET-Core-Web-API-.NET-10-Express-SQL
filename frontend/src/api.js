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

  let response;
  try {
    response = await fetch(`${BASE_URL}${endpoint}`, {
      ...options,
      headers,
    });
  } catch (netErr) {
    const error = new Error('Unable to connect to the Operix API server. Please verify your connection or ensure the backend is running.');
    error.status = 0;
    error.originalError = netErr;
    throw error;
  }

  if (response.status === 204) {
    return null;
  }

  let data = null;
  const contentType = response.headers.get('content-type');
  if (contentType && contentType.includes('application/json')) {
    try {
      data = await response.json();
    } catch {
      data = null;
    }
  } else {
    try {
      data = await response.text();
    } catch {
      data = null;
    }
  }

  if (!response.ok) {
    if (response.status === 401) {
      setAuthToken(null);
      setSavedUser(null);
    }

    let errorMsg = '';
    let fieldErrors = null;

    if (data && typeof data === 'object') {
      if (data.errors && typeof data.errors === 'object') {
        fieldErrors = data.errors;
        const errorList = Object.entries(data.errors)
          .map(([_, msgs]) => (Array.isArray(msgs) ? msgs.join(', ') : msgs))
          .filter(Boolean);
        if (errorList.length > 0) {
          errorMsg = errorList.join('; ');
        }
      }
      if (!errorMsg) {
        errorMsg = data.detail || data.title || data.message || '';
      }
    } else if (typeof data === 'string' && data.trim()) {
      errorMsg = data.trim();
    }

    if (!errorMsg) {
      if (response.status === 400) errorMsg = 'Invalid request. Please verify your submitted information.';
      else if (response.status === 401) errorMsg = 'Your session has expired or you are unauthorized. Please sign in.';
      else if (response.status === 403) errorMsg = 'Access denied. You do not have permission to perform this action.';
      else if (response.status === 404) errorMsg = 'The requested resource was not found.';
      else if (response.status === 409) errorMsg = 'A conflict occurred while processing the request.';
      else if (response.status >= 500) errorMsg = 'An internal server error occurred. Please try again later.';
      else errorMsg = 'An unexpected error occurred.';
    }

    const error = new Error(errorMsg);
    error.status = response.status;
    error.details = data;
    error.fieldErrors = fieldErrors;
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
      const page = params.page || params.pageNumber;
      if (page) searchParams.set('page', page);
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
      const page = params.page || params.pageNumber;
      if (page) searchParams.set('page', page);
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
        body: JSON.stringify({ notes: reason, rejectionReason: reason }),
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
      const page = params.page || params.pageNumber;
      if (page) searchParams.set('page', page);
      if (params.pageSize) searchParams.set('pageSize', params.pageSize);
      if (params.status) searchParams.set('status', params.status);
      if (params.priority) searchParams.set('priority', params.priority);
      const assignee = params.assignedToId || params.assigneeId;
      if (assignee) searchParams.set('assignedToId', assignee);
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
      const page = params.page || params.pageNumber;
      if (page) searchParams.set('page', page);
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
        body: JSON.stringify({ notes: reason, rejectionReason: reason }),
      }),
    cancel: (id) =>
      request(`/expenses/${id}/cancel`, {
        method: 'POST',
      }),
  },
};
