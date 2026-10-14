import React, { useState, useEffect } from 'react';
import { api, getAuthToken, setAuthToken, getSavedUser, setSavedUser } from './api';
import './App.css';
import {
  LayoutDashboard,
  Users,
  CalendarDays,
  CheckSquare,
  Receipt,
  LogOut,
  Plus,
  Search,
  CheckCircle2,
  XCircle,
  Clock,
  AlertCircle,
  Building2,
  UserCheck,
  ShieldCheck,
  ArrowRight,
  Eye,
  EyeOff
} from 'lucide-react';

const getTodayDate = () => new Date().toISOString().split('T')[0];
const getFutureDate = (days) => new Date(Date.now() + 86400000 * days).toISOString().split('T')[0];

const LEAVE_STATUSES = {
  1: 'Pending',
  2: 'Approved',
  3: 'Rejected',
  4: 'Cancelled'
};

const EXPENSE_STATUSES = {
  1: 'Pending',
  2: 'Approved',
  3: 'Rejected',
  4: 'Cancelled'
};

const EXPENSE_CATEGORIES = {
  1: 'Travel',
  2: 'Meals',
  3: 'Equipment',
  4: 'Office Supplies',
  5: 'Training',
  6: 'Other'
};

const formatLeaveStatus = (val) => {
  if (val === null || val === undefined) return '—';
  if (typeof val === 'number') return LEAVE_STATUSES[val] || `Status ${val}`;
  return String(val);
};

const formatExpenseStatus = (val) => {
  if (val === null || val === undefined) return '—';
  if (typeof val === 'number') return EXPENSE_STATUSES[val] || `Status ${val}`;
  return String(val);
};

const formatExpenseCategory = (val) => {
  if (val === null || val === undefined) return '—';
  if (typeof val === 'number') return EXPENSE_CATEGORIES[val] || `Category ${val}`;
  return String(val);
};

const TASK_STATUS_MAP = {
  1: 'Todo',
  2: 'InProgress',
  3: 'InReview',
  4: 'Done',
  5: 'Cancelled'
};

const TASK_PRIORITY_MAP = {
  0: 'Low',
  1: 'Medium',
  2: 'High',
  3: 'Urgent',
  4: 'Critical'
};

const normalizeTaskStatus = (s) => {
  if (typeof s === 'number') return TASK_STATUS_MAP[s] || 'Todo';
  return s || 'Todo';
};

const normalizeTaskPriority = (p) => {
  if (typeof p === 'number') return TASK_PRIORITY_MAP[p] || 'Medium';
  return p || 'Medium';
};

const isTestDepartment = (dept) => {
  const name = dept?.name || '';
  return name.startsWith('BogusDept_') || name.startsWith('Dept_') || name.startsWith('TaskDept_') || name.startsWith('LeaveDept_') || name.startsWith('ExpDept_');
};

const isTestEmployee = (emp) => {
  const email = (emp?.email || '').toLowerCase();
  const first = (emp?.firstName || '').toLowerCase();
  const last = (emp?.lastName || '').toLowerCase();
  return email.endsWith('@test.com') || email.includes('dupe_emp_') || (first === 'bob' && last === 'builder');
};

export default function App() {
  const [currentUser, setCurrentUser] = useState(getSavedUser());
  const [activeTab, setActiveTab] = useState('dashboard');
  const [healthStatus, setHealthStatus] = useState({ online: false, db: 'Unknown', latency: null });
  const [toasts, setToasts] = useState([]);

  // Auth modal & security controls
  const [showAuthModal, setShowAuthModal] = useState(false);
  const [authMode, setAuthMode] = useState('login'); // 'login' or 'register'
  const [authForm, setAuthForm] = useState({ firstName: '', lastName: '', email: '', password: '', role: 'Employee' });
  const [showPassword, setShowPassword] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [authError, setAuthError] = useState('');
  const [authFieldErrors, setAuthFieldErrors] = useState({});

  // Dashboard Data
  const [summaryData, setSummaryData] = useState(null);
  const [personalData, setPersonalData] = useState(null);

  // Departments & Employees
  const [departments, setDepartments] = useState([]);
  const [deptSearch, setDeptSearch] = useState('');
  const [employees, setEmployees] = useState([]);
  const [employeeSearch, setEmployeeSearch] = useState('');
  const [employeeDeptFilter, setEmployeeDeptFilter] = useState('');
  const [hideTestFixtures, setHideTestFixtures] = useState(true);

  // Leaves
  const [myLeaves, setMyLeaves] = useState([]);
  const [reviewLeaves, setReviewLeaves] = useState([]);
  const [leaveSubTab, setLeaveSubTab] = useState('my'); // 'my' or 'review'

  // Tasks
  const [tasks, setTasks] = useState([]);
  const [taskFilterStatus, setTaskFilterStatus] = useState('');
  const [taskFilterPriority, setTaskFilterPriority] = useState('');
  const [taskSearch, setTaskSearch] = useState('');

  // Expenses
  const [myExpenses, setMyExpenses] = useState([]);
  const [reviewExpenses, setReviewExpenses] = useState([]);
  const [expenseSubTab, setExpenseSubTab] = useState('my'); // 'my' or 'review'

  // Modals
  const [modalOpen, setModalOpen] = useState(null); // 'dept', 'employee', 'leave', 'task', 'expense', 'rejectLeave', 'rejectExpense'
  const [rejectTargetId, setRejectTargetId] = useState(null);
  const [rejectionReason, setRejectionReason] = useState('');
  const [modalError, setModalError] = useState('');

  // Forms
  const [newDeptForm, setNewDeptForm] = useState({ name: '', description: '' });
  const [newEmployeeForm, setNewEmployeeForm] = useState(() => ({
    firstName: '',
    lastName: '',
    email: '',
    departmentId: '',
    managerId: '',
    hireDate: getTodayDate(),
    annualLeaveBalanceDays: 20
  }));
  const [newLeaveForm, setNewLeaveForm] = useState(() => ({
    startDate: getTodayDate(),
    endDate: getFutureDate(3),
    reason: ''
  }));
  const [newTaskForm, setNewTaskForm] = useState(() => ({
    title: '',
    description: '',
    priority: 1, // Medium
    assignedToEmployeeId: '',
    dueDate: getFutureDate(7)
  }));
  const [newExpenseForm, setNewExpenseForm] = useState(() => ({
    amount: 50.0,
    currency: 'USD',
    category: 1, // Travel
    expenseDate: getTodayDate(),
    description: ''
  }));

  const showToast = (message, type = 'success') => {
    const id = Date.now();
    setToasts((prev) => [...prev, { id, message, type }]);
    setTimeout(() => {
      setToasts((prev) => prev.filter((t) => t.id !== id));
    }, 4000);
  };

  // Health check polling
  useEffect(() => {
    const checkHealth = async () => {
      const start = performance.now();
      try {
        const res = await api.health();
        const latency = Math.round(performance.now() - start);
        setHealthStatus({
          online: res.status === 'Healthy',
          db: res.database || 'Connected',
          latency
        });
      } catch (err) {
        setHealthStatus({ online: false, db: 'Offline', latency: null });
      }
    };
    checkHealth();
    const interval = setInterval(checkHealth, 10000);
    return () => clearInterval(interval);
  }, []);

  // Automatically redirect employees away from management-only tabs
  useEffect(() => {
    if (currentUser?.role === 'Employee' && activeTab === 'employees') {
      setActiveTab('dashboard');
    }
  }, [activeTab, currentUser]);

  // Fetch initial core data
  useEffect(() => {
    if (currentUser) {
      loadUserData();
    }
  }, [currentUser]);

  const refreshDashboard = () => {
    try {
      if (currentUser?.role === 'Admin' || currentUser?.role === 'Manager') {
        api.dashboard.summary().then(setSummaryData).catch(() => {});
      }
      api.dashboard.my().then(setPersonalData).catch(() => {});
    } catch (err) {
      console.error('Failed to refresh dashboard', err);
    }
  };

  const loadUserData = async () => {
    try {
      refreshDashboard();
      api.departments.list().then(setDepartments).catch(() => {});
      if (currentUser?.role === 'Admin' || currentUser?.role === 'Manager') {
        api.employees.list({ pageSize: 50 }).then((res) => setEmployees(res.items || [])).catch(() => {});
      } else {
        setEmployees([]);
      }
      loadLeaves();
      loadTasks();
      loadExpenses();
    } catch (err) {
      console.error(err);
    }
  };

  const loadLeaves = async () => {
    try {
      const my = await api.leave.my();
      setMyLeaves(my || []);
      if (currentUser?.role === 'Admin' || currentUser?.role === 'Manager') {
        const rev = await api.leave.list({ pageSize: 50, status: 'Pending' });
        setReviewLeaves(rev.items || []);
      } else {
        setReviewLeaves([]);
      }
    } catch (e) { console.error(e); }
  };

  const loadTasks = async () => {
    try {
      const params = {
        pageSize: 100,
        status: taskFilterStatus || undefined,
        priority: taskFilterPriority || undefined,
        search: taskSearch || undefined
      };
      if (currentUser?.role === 'Employee' && personalData?.employeeId) {
        params.assignedToId = personalData.employeeId;
      }
      const res = await api.tasks.list(params);
      setTasks(res.items || []);
    } catch (e) { console.error(e); }
  };

  const loadExpenses = async () => {
    try {
      const my = await api.expenses.my();
      setMyExpenses(my || []);
      if (currentUser?.role === 'Admin' || currentUser?.role === 'Manager') {
        const rev = await api.expenses.list({ pageSize: 50, status: 'Pending' });
        setReviewExpenses(rev.items || []);
      } else {
        setReviewExpenses([]);
      }
    } catch (e) { console.error(e); }
  };

  useEffect(() => {
    if (currentUser) {
      loadTasks();
    }
  }, [currentUser, personalData?.employeeId, taskFilterStatus, taskFilterPriority, taskSearch]);

  // Demo account population (does NOT auto-login, requires explicit Sign In click)
  const fillDemoCredentials = (email, password) => {
    setAuthMode('login');
    setAuthForm((prev) => ({ ...prev, email, password }));
    setAuthError('');
    setAuthFieldErrors({});
    showToast(`Loaded ${email} credentials. Click "Sign In" to authenticate.`);
  };

  // Auth Actions
  const handleLogin = async (e) => {
    if (e) e.preventDefault();
    if (isSubmitting) return;

    const email = authForm.email.trim();
    const password = authForm.password;

    const errors = {};
    if (!email) errors.email = 'Email address or username is required.';
    if (!password) errors.password = 'Password is required.';

    if (Object.keys(errors).length > 0) {
      setAuthFieldErrors(errors);
      setAuthError('Please fill in all required credentials.');
      return;
    }

    setIsSubmitting(true);
    setAuthError('');
    setAuthFieldErrors({});

    try {
      const res = await api.auth.login(email, password);
      setAuthToken(res.token);
      const userObj = {
        id: res.userId,
        email: res.email,
        role: res.role,
        employeeId: res.employeeId,
        username: res.email.split('@')[0]
      };
      setSavedUser(userObj);
      setCurrentUser(userObj);
      setShowAuthModal(false);
      setAuthForm({ firstName: '', lastName: '', email: '', password: '', role: 'Employee' });
      showToast(`Welcome back, ${userObj.username} (${userObj.role})!`);
    } catch (err) {
      setAuthError(err.message || 'Authentication failed. Please verify your credentials.');
      setAuthFieldErrors(err.fieldErrors || {});
      showToast(err.message, 'error');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleRegister = async (e) => {
    if (e) e.preventDefault();
    if (isSubmitting) return;

    const firstName = authForm.firstName.trim();
    const lastName = authForm.lastName.trim();
    const email = authForm.email.trim();
    const password = authForm.password;

    const errors = {};
    if (!firstName) errors.firstName = 'First name is required.';
    if (!lastName) errors.lastName = 'Last name is required.';
    if (!email) {
      errors.email = 'Email address is required.';
    } else if (!email.includes('@') || !email.includes('.')) {
      errors.email = 'Please enter a valid email address.';
    }

    if (!password) {
      errors.password = 'Password is required.';
    } else if (password.length < 8) {
      errors.password = 'Password must be at least 8 characters long.';
    } else if (
      !/[A-Z]/.test(password) ||
      !/[a-z]/.test(password) ||
      !/[0-9]/.test(password) ||
      !/[!*@#$%^&+=]/.test(password)
    ) {
      errors.password = 'Password must include uppercase, lowercase, digit, and symbol.';
    }

    if (Object.keys(errors).length > 0) {
      setAuthFieldErrors(errors);
      setAuthError('Please fix the validation errors before registering.');
      return;
    }

    setIsSubmitting(true);
    setAuthError('');
    setAuthFieldErrors({});

    try {
      const roleMap = { Admin: 3, Manager: 2, Employee: 1 };
      const res = await api.auth.register({
        firstName,
        lastName,
        email,
        password,
        role: roleMap[authForm.role] || 1
      });
      setAuthToken(res.token);
      const userObj = {
        id: res.userId,
        email: res.email,
        role: res.role,
        employeeId: res.employeeId,
        username: res.email.split('@')[0]
      };
      setSavedUser(userObj);
      setCurrentUser(userObj);
      setShowAuthModal(false);
      setAuthForm({ firstName: '', lastName: '', email: '', password: '', role: 'Employee' });
      showToast(`Account registered successfully as ${userObj.role}!`);
    } catch (err) {
      setAuthError(err.message || 'Registration failed. Please check the inputs.');
      setAuthFieldErrors(err.fieldErrors || {});
      showToast(err.message, 'error');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleLogout = () => {
    setAuthToken(null);
    setSavedUser(null);
    setCurrentUser(null);
    setSummaryData(null);
    setPersonalData(null);
    showToast('Logged out successfully');
  };

  // Actions
  const handleCreateDepartment = async (e) => {
    e.preventDefault();
    if (isSubmitting) return;

    if (!newDeptForm.name.trim()) {
      showToast('Department name is required.', 'error');
      return;
    }

    setIsSubmitting(true);
    setModalError('');
    try {
      await api.departments.create({
        name: newDeptForm.name.trim(),
        description: newDeptForm.description.trim() || undefined
      });
      showToast('Department created successfully!');
      setModalOpen(null);
      setNewDeptForm({ name: '', description: '' });
      const depts = await api.departments.list();
      setDepartments(depts);
      refreshDashboard();
    } catch (err) {
      setModalError(err.message);
      showToast(err.message, 'error');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleCreateEmployee = async (e) => {
    e.preventDefault();
    if (isSubmitting) return;

    if (!newEmployeeForm.firstName.trim() || !newEmployeeForm.lastName.trim() || !newEmployeeForm.email.trim()) {
      showToast('First name, last name, and email are required.', 'error');
      return;
    }
    if (!newEmployeeForm.departmentId) {
      showToast('Please select a department.', 'error');
      return;
    }
    if (!newEmployeeForm.hireDate) {
      showToast('Hire date is required.', 'error');
      return;
    }

    setIsSubmitting(true);
    setModalError('');
    try {
      await api.employees.create({
        firstName: newEmployeeForm.firstName.trim(),
        lastName: newEmployeeForm.lastName.trim(),
        email: newEmployeeForm.email.trim(),
        departmentId: newEmployeeForm.departmentId,
        managerId: newEmployeeForm.managerId || undefined,
        hireDate: newEmployeeForm.hireDate,
        annualLeaveBalanceDays: Number(newEmployeeForm.annualLeaveBalanceDays) || 20
      });
      showToast('Employee profile added successfully!');
      setModalOpen(null);
      setNewEmployeeForm({
        firstName: '',
        lastName: '',
        email: '',
        departmentId: '',
        managerId: '',
        hireDate: getTodayDate(),
        annualLeaveBalanceDays: 20
      });
      const res = await api.employees.list({ pageSize: 50 });
      setEmployees(res.items || []);
      refreshDashboard();
    } catch (err) {
      setModalError(err.message);
      showToast(err.message, 'error');
    } finally {
      setIsSubmitting(false);
    }
  };

  const isLeaveDateConflicted = (startStr, endStr) => {
    if (!startStr || !endStr) return false;
    const s = new Date(startStr);
    const e = new Date(endStr);
    return myLeaves.some((lr) => {
      const status = formatLeaveStatus(lr.status);
      if (status !== 'Pending' && status !== 'Approved') return false;
      const lrS = new Date(lr.startDate);
      const lrE = new Date(lr.endDate);
      return s <= lrE && e >= lrS;
    });
  };

  const handleApplyLeave = async (e) => {
    e.preventDefault();
    if (isSubmitting) return;

    if (!newLeaveForm.startDate || !newLeaveForm.endDate) {
      showToast('Start and end dates are required.', 'error');
      return;
    }
    if (new Date(newLeaveForm.startDate) > new Date(newLeaveForm.endDate)) {
      showToast('Start date cannot be after end date.', 'error');
      return;
    }
    if (!newLeaveForm.reason.trim()) {
      showToast('Reason for leave is required.', 'error');
      return;
    }

    if (isLeaveDateConflicted(newLeaveForm.startDate, newLeaveForm.endDate)) {
      setModalError('Employee already has an active or pending leave request covering these dates. Please choose different dates.');
      showToast('Selected dates conflict with an existing leave request.', 'error');
      return;
    }

    setIsSubmitting(true);
    setModalError('');
    try {
      await api.leave.submit({
        startDate: newLeaveForm.startDate,
        endDate: newLeaveForm.endDate,
        reason: newLeaveForm.reason.trim()
      });
      showToast('Time-off request submitted!');
      setModalOpen(null);
      setNewLeaveForm({
        startDate: getFutureDate(7),
        endDate: getFutureDate(10),
        reason: ''
      });
      loadLeaves();
      refreshDashboard();
    } catch (err) {
      setModalError(err.message);
      showToast(err.message, 'error');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleApproveLeave = async (id) => {
    try {
      await api.leave.approve(id);
      showToast('Leave request approved! Balance updated.');
      loadLeaves();
      refreshDashboard();
    } catch (err) {
      showToast(err.message, 'error');
    }
  };

  const handleRejectLeaveConfirm = async () => {
    const reason = rejectionReason.trim();
    if (!reason) {
      setModalError('A rejection reason is required.');
      showToast('A rejection reason is required.', 'error');
      return;
    }
    setIsSubmitting(true);
    setModalError('');
    try {
      await api.leave.reject(rejectTargetId, reason);
      showToast('Leave request rejected.');
      setModalOpen(null);
      setRejectionReason('');
      loadLeaves();
      refreshDashboard();
    } catch (err) {
      setModalError(err.message || 'Failed to reject leave request.');
      showToast(err.message, 'error');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleCancelLeave = async (id) => {
    try {
      await api.leave.cancel(id);
      showToast('Leave request cancelled.');
      loadLeaves();
      refreshDashboard();
    } catch (err) {
      showToast(err.message, 'error');
    }
  };

  const handleCreateTask = async (e) => {
    e.preventDefault();
    if (isSubmitting) return;

    if (!newTaskForm.title.trim()) {
      showToast('Task title is required.', 'error');
      return;
    }

    setIsSubmitting(true);
    setModalError('');
    try {
      await api.tasks.create({
        title: newTaskForm.title.trim(),
        description: newTaskForm.description.trim() || undefined,
        priority: Number(newTaskForm.priority),
        assignedToEmployeeId: newTaskForm.assignedToEmployeeId || undefined,
        dueDate: newTaskForm.dueDate || undefined
      });
      showToast('Task created successfully!');
      setModalOpen(null);
      setNewTaskForm({
        title: '',
        description: '',
        priority: 1,
        assignedToEmployeeId: '',
        dueDate: getFutureDate(7)
      });
      loadTasks();
      refreshDashboard();
    } catch (err) {
      setModalError(err.message);
      showToast(err.message, 'error');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleTaskAction = async (taskId, action) => {
    try {
      if (action === 'start') await api.tasks.start(taskId);
      else if (action === 'review') await api.tasks.review(taskId);
      else if (action === 'requestChanges') await api.tasks.requestChanges(taskId);
      else if (action === 'complete') await api.tasks.complete(taskId);
      else if (action === 'cancel') await api.tasks.cancel(taskId);
      showToast('Task status updated!');
      loadTasks();
      refreshDashboard();
    } catch (err) {
      showToast(err.message, 'error');
    }
  };

  const handleSubmitExpense = async (e) => {
    e.preventDefault();
    if (isSubmitting) return;

    const amount = Number(newExpenseForm.amount);
    if (!amount || amount <= 0) {
      showToast('Please enter a valid expense amount greater than 0.', 'error');
      return;
    }
    if (!newExpenseForm.description.trim()) {
      showToast('Expense justification/description is required.', 'error');
      return;
    }

    setIsSubmitting(true);
    setModalError('');
    try {
      await api.expenses.submit({
        amount,
        currency: newExpenseForm.currency,
        category: Number(newExpenseForm.category),
        expenseDate: newExpenseForm.expenseDate,
        description: newExpenseForm.description.trim()
      });
      showToast('Expense reimbursement submitted!');
      setModalOpen(null);
      setNewExpenseForm({
        amount: 50.0,
        currency: 'USD',
        category: 1,
        expenseDate: getTodayDate(),
        description: ''
      });
      loadExpenses();
      refreshDashboard();
    } catch (err) {
      setModalError(err.message);
      showToast(err.message, 'error');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleApproveExpense = async (id) => {
    try {
      await api.expenses.approve(id);
      showToast('Expense approved for reimbursement!');
      loadExpenses();
      refreshDashboard();
    } catch (err) {
      showToast(err.message, 'error');
    }
  };

  const handleRejectExpenseConfirm = async () => {
    const reason = rejectionReason.trim();
    if (!reason) {
      setModalError('A rejection reason is required.');
      showToast('A rejection reason is required.', 'error');
      return;
    }
    setIsSubmitting(true);
    setModalError('');
    try {
      await api.expenses.reject(rejectTargetId, reason);
      showToast('Expense reimbursement rejected.');
      setModalOpen(null);
      setRejectionReason('');
      loadExpenses();
      refreshDashboard();
    } catch (err) {
      setModalError(err.message || 'Failed to reject expense reimbursement.');
      showToast(err.message, 'error');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleCancelExpense = async (id) => {
    try {
      await api.expenses.cancel(id);
      showToast('Expense reimbursement cancelled.');
      loadExpenses();
      refreshDashboard();
    } catch (err) {
      showToast(err.message, 'error');
    }
  };

  const filteredEmployees = employees.filter((emp) => {
    if (hideTestFixtures && isTestEmployee(emp)) return false;
    const fullName = `${emp.firstName || ''} ${emp.lastName || ''}`.trim().toLowerCase();
    const email = (emp.email || '').toLowerCase();
    const deptName = (emp.departmentName || '').toLowerCase();
    const search = employeeSearch.toLowerCase().trim();
    const matchesSearch =
      !search ||
      fullName.includes(search) ||
      email.includes(search) ||
      deptName.includes(search);
    const matchesDept = !employeeDeptFilter || emp.departmentId === employeeDeptFilter;
    return matchesSearch && matchesDept;
  });

  const filteredDepartments = departments.filter((dept) => {
    if (hideTestFixtures && isTestDepartment(dept)) return false;
    const name = (dept.name || '').toLowerCase();
    const mgr = (dept.managerName || '').toLowerCase();
    const search = deptSearch.toLowerCase().trim();
    return !search || name.includes(search) || mgr.includes(search);
  });

  const assignableEmployees = employees.filter((emp) => !hideTestFixtures || !isTestEmployee(emp));
  const testDeptCount = departments.filter(isTestDepartment).length;

  return (
    <div className="app-container">
      {/* Navbar */}
      <header className="navbar">
        <div className="nav-brand" onClick={() => setActiveTab('dashboard')}>
          <div className="brand-icon">
            <ShieldCheck size={22} />
          </div>
          <div>
            <span className="brand-title">OPERIX</span>
            <span className="brand-tag">v1.0 Pro</span>
          </div>
        </div>

        <div className="nav-actions">
          <div className={`health-pill ${healthStatus.online ? '' : 'offline'}`} title="Real-time backend API & DB Status">
            <span className="health-dot" />
            <span>{healthStatus.online ? `API Online (${healthStatus.latency}ms)` : 'Backend Offline'}</span>
          </div>

          {currentUser ? (
            <div className="user-badge">
              <div className="user-avatar">{currentUser.username[0].toUpperCase()}</div>
              <div className="user-info">
                <span className="user-name">{currentUser.username}</span>
                <span className="role-pill">{currentUser.role}</span>
              </div>
              <button
                className="btn-secondary btn-sm"
                onClick={handleLogout}
                title="Logout"
                style={{ padding: '0.3rem 0.5rem', marginLeft: '0.5rem' }}
              >
                <LogOut size={14} />
              </button>
            </div>
          ) : (
            <button className="btn-primary" onClick={() => setShowAuthModal(true)}>
              Sign In / Switch Role
            </button>
          )}
        </div>
      </header>

      {/* Main Navigation Tabs */}
      <div className="main-content">
        <nav className="tabs-bar">
          <button
            className={`tab-btn ${activeTab === 'dashboard' ? 'active' : ''}`}
            onClick={() => setActiveTab('dashboard')}
          >
            <LayoutDashboard size={18} /> Overview
          </button>
          {(currentUser?.role === 'Admin' || currentUser?.role === 'Manager') && (
            <button
              className={`tab-btn ${activeTab === 'employees' ? 'active' : ''}`}
              onClick={() => setActiveTab('employees')}
            >
              <Users size={18} /> Personnel & Teams
            </button>
          )}
          <button
            className={`tab-btn ${activeTab === 'leaves' ? 'active' : ''}`}
            onClick={() => setActiveTab('leaves')}
          >
            <CalendarDays size={18} /> Time Off & Leaves
          </button>
          <button
            className={`tab-btn ${activeTab === 'tasks' ? 'active' : ''}`}
            onClick={() => setActiveTab('tasks')}
          >
            <CheckSquare size={18} /> Task Operations
          </button>
          <button
            className={`tab-btn ${activeTab === 'expenses' ? 'active' : ''}`}
            onClick={() => setActiveTab('expenses')}
          >
            <Receipt size={18} /> Reimbursements
          </button>
        </nav>

        {/* TAB 1: OVERVIEW DASHBOARD */}
        {activeTab === 'dashboard' && (
          <div>
            <div className="page-header">
              <div>
                <h1 className="page-title">
                  {currentUser?.role === 'Admin'
                    ? 'Executive Operations Command Center'
                    : currentUser?.role === 'Manager'
                    ? 'Operations Command Center'
                    : 'Employee Workspace'}
                </h1>
                <p className="page-subtitle">
                  {currentUser?.role === 'Admin'
                    ? 'Real-time organizational performance metrics, workflows, and workforce telemetry.'
                    : currentUser?.role === 'Manager'
                    ? 'Departmental throughput, team deliverables, and operational telemetry.'
                    : 'Your active deliverables, leave allowances, and personal expenditure telemetry.'}
                </p>
              </div>
              <div className="header-actions">
                {currentUser?.role === 'Admin' && (
                  <>
                    <button className="btn-primary" onClick={() => setModalOpen('task')}>
                      <Plus size={16} /> Create Task
                    </button>
                    <button className="btn-secondary" onClick={() => setModalOpen('employee')}>
                      <Plus size={16} /> Add Employee
                    </button>
                    <button className="btn-secondary" onClick={() => setModalOpen('dept')}>
                      <Plus size={16} /> Add Department
                    </button>
                  </>
                )}
                {currentUser?.role === 'Manager' && (
                  <>
                    <button className="btn-primary" onClick={() => setModalOpen('task')}>
                      <Plus size={16} /> Create Task
                    </button>
                    <button className="btn-secondary" onClick={() => setModalOpen('leave')}>
                      <Plus size={16} /> Apply Time Off
                    </button>
                    <button className="btn-secondary" onClick={() => setModalOpen('expense')}>
                      <Plus size={16} /> Claim Expense
                    </button>
                  </>
                )}
                {currentUser?.role === 'Employee' && (
                  <>
                    <button className="btn-primary" onClick={() => setModalOpen('leave')}>
                      <Plus size={16} /> Apply Time Off
                    </button>
                    <button className="btn-secondary" onClick={() => setModalOpen('expense')}>
                      <Plus size={16} /> Claim Expense
                    </button>
                  </>
                )}
              </div>
            </div>

            {/* Executive Summary Stats (Admin & Manager) */}
            {(currentUser?.role === 'Admin' || currentUser?.role === 'Manager') && (
              summaryData ? (
                <div className="stats-grid">
                  <div className="glass-card stat-card">
                    <div className="stat-icon indigo"><Users size={26} /></div>
                    <div className="stat-content">
                      <span className="stat-label">Active Employees</span>
                      <span className="stat-value">{summaryData.totalEmployees ?? summaryData.activeEmployees ?? 0}</span>
                      <span className="stat-sub">Across {summaryData.totalDepartments ?? departments.length} Departments</span>
                    </div>
                  </div>

                  <div className="glass-card stat-card">
                    <div className="stat-icon cyan"><CheckSquare size={26} /></div>
                    <div className="stat-content">
                      <span className="stat-label">Active Tasks</span>
                      <span className="stat-value">
                        {(summaryData.todoTasks || 0) + (summaryData.inProgressTasks || 0) + (summaryData.inReviewTasks || 0)}
                      </span>
                      <span className="stat-sub">{summaryData.completedTasks ?? 0} completed</span>
                    </div>
                  </div>

                  <div className="glass-card stat-card">
                    <div className="stat-icon emerald"><CalendarDays size={26} /></div>
                    <div className="stat-content">
                      <span className="stat-label">Pending Leaves</span>
                      <span className="stat-value">{summaryData.pendingLeaveRequests ?? summaryData.totalPendingLeaves ?? 0}</span>
                      <span className="stat-sub">{summaryData.approvedLeaveRequests ?? summaryData.totalApprovedLeaves ?? 0} approved YTD</span>
                    </div>
                  </div>

                  <div className="glass-card stat-card">
                    <div className="stat-icon amber"><Receipt size={26} /></div>
                    <div className="stat-content">
                      <span className="stat-label">Pending Expenses</span>
                      <span className="stat-value">${Number(summaryData.totalPendingExpensesAmount ?? 0).toFixed(2)}</span>
                      <span className="stat-sub">${Number(summaryData.totalApprovedExpensesAmount ?? 0).toFixed(2)} approved</span>
                    </div>
                  </div>
                </div>
              ) : (
                <div className="glass-card" style={{ marginBottom: '2rem', padding: '1.25rem', textAlign: 'center' }}>
                  <span style={{ color: 'var(--text-muted)' }}>Loading executive telemetry...</span>
                </div>
              )
            )}

            {/* Employee Role Personal Workspace Stats */}
            {currentUser?.role === 'Employee' && (
              <div className="stats-grid">
                <div className="glass-card stat-card">
                  <div className="stat-icon emerald"><CalendarDays size={26} /></div>
                  <div className="stat-content">
                    <span className="stat-label">Annual Leave Balance</span>
                    <span className="stat-value">{personalData?.annualLeaveBalanceDays ?? 20}</span>
                    <span className="stat-sub">Paid days remaining</span>
                  </div>
                </div>

                <div className="glass-card stat-card">
                  <div className="stat-icon cyan"><CheckSquare size={26} /></div>
                  <div className="stat-content">
                    <span className="stat-label">My Assigned Tasks</span>
                    <span className="stat-value">
                      {tasks.filter((t) => normalizeTaskStatus(t.status) !== 'Done' && normalizeTaskStatus(t.status) !== 'Cancelled').length}
                    </span>
                    <span className="stat-sub">{tasks.filter((t) => normalizeTaskStatus(t.status) === 'Done').length} completed</span>
                  </div>
                </div>

                <div className="glass-card stat-card">
                  <div className="stat-icon indigo"><CalendarDays size={26} /></div>
                  <div className="stat-content">
                    <span className="stat-label">My Leave Requests</span>
                    <span className="stat-value">{myLeaves.filter((l) => formatLeaveStatus(l.status) === 'Pending').length}</span>
                    <span className="stat-sub">Pending supervisor review</span>
                  </div>
                </div>

                <div className="glass-card stat-card">
                  <div className="stat-icon amber"><Receipt size={26} /></div>
                  <div className="stat-content">
                    <span className="stat-label">My Pending Reimbursements</span>
                    <span className="stat-value">
                      ${myExpenses.filter((e) => formatExpenseStatus(e.status) === 'Pending').reduce((s, e) => s + Number(e.amount || 0), 0).toFixed(2)}
                    </span>
                    <span className="stat-sub">{myExpenses.filter((e) => formatExpenseStatus(e.status) === 'Pending').length} claims in review</span>
                  </div>
                </div>
              </div>
            )}

            {/* Manager / Admin Personal Profile Widget (if available) */}
            {currentUser?.role !== 'Employee' && personalData && (
              <div className="glass-card" style={{ marginBottom: '2rem' }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem', flexWrap: 'wrap', gap: '0.5rem' }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
                    <div className="user-avatar" style={{ width: 44, height: 44, fontSize: '1.1rem' }}>
                      {personalData.fullName?.[0] || 'U'}
                    </div>
                    <div>
                      <h3 style={{ fontSize: '1.15rem' }}>{personalData.fullName}</h3>
                      <p style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>
                        {currentUser.role} • {personalData.departmentName || 'Operations'}
                      </p>
                    </div>
                  </div>
                  <div style={{ display: 'flex', gap: '1rem' }}>
                    <div style={{ textAlign: 'right' }}>
                      <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)', textTransform: 'uppercase' }}>Annual Leave Balance</span>
                      <div style={{ fontSize: '1.35rem', fontWeight: 800, color: '#34d399' }}>
                        {personalData.annualLeaveBalanceDays ?? 0} <span style={{ fontSize: '0.85rem' }}>Days Left</span>
                      </div>
                    </div>
                  </div>
                </div>

                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))', gap: '1rem', borderTop: '1px solid var(--border-subtle)', paddingTop: '1rem' }}>
                  <div>
                    <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>Assigned Deliverables</span>
                    <div style={{ fontSize: '1.1rem', fontWeight: 700, marginTop: '0.2rem' }}>
                      {(personalData.inProgressTasksCount || 0) + (personalData.assignedTasksCount || 0)} In Flight
                    </div>
                  </div>
                  <div>
                    <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>Pending Leave Requests</span>
                    <div style={{ fontSize: '1.1rem', fontWeight: 700, marginTop: '0.2rem' }}>
                      {personalData.pendingLeaveRequests ?? 0} Pending
                    </div>
                  </div>
                  <div>
                    <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>Expense Claims</span>
                    <div style={{ fontSize: '1.1rem', fontWeight: 700, marginTop: '0.2rem' }}>
                      ${Number(personalData.totalPendingExpensesAmount ?? 0).toFixed(2)} Pending
                    </div>
                  </div>
                </div>
              </div>
            )}

            {/* Employee Active Assignments Showcase */}
            {currentUser?.role === 'Employee' && (
              <div style={{ marginTop: '2rem' }}>
                <h3 style={{ marginBottom: '1rem', fontSize: '1.1rem' }}>My Active Assignments</h3>
                {tasks.filter((t) => normalizeTaskStatus(t.status) !== 'Done' && normalizeTaskStatus(t.status) !== 'Cancelled').length === 0 ? (
                  <div className="glass-card" style={{ padding: '2rem', textAlign: 'center', color: 'var(--text-muted)' }}>
                    You currently have no tasks in flight. All deliverables are complete!
                  </div>
                ) : (
                  <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))', gap: '1rem' }}>
                    {tasks
                      .filter((t) => normalizeTaskStatus(t.status) !== 'Done' && normalizeTaskStatus(t.status) !== 'Cancelled')
                      .slice(0, 6)
                      .map((t) => (
                        <div key={t.id} className="task-card">
                          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                            <span className={`badge badge-${normalizeTaskPriority(t.priority).toLowerCase()}`}>
                              {normalizeTaskPriority(t.priority)}
                            </span>
                            <span className={`badge badge-${normalizeTaskStatus(t.status).toLowerCase()}`}>
                              {normalizeTaskStatus(t.status)}
                            </span>
                          </div>
                          <h4 style={{ fontSize: '0.95rem', fontWeight: 600, marginTop: '0.5rem' }}>{t.title}</h4>
                          {t.description && (
                            <p style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>{t.description}</p>
                          )}
                          <div style={{ fontSize: '0.75rem', color: 'var(--text-dim)', marginTop: '0.5rem' }}>
                            Due: {t.dueDate?.split('T')[0] || 'No date set'}
                          </div>
                          <div className="task-actions" style={{ marginTop: '0.75rem' }}>
                            {normalizeTaskStatus(t.status) === 'Todo' && (
                              <button className="btn-secondary btn-sm" onClick={() => handleTaskAction(t.id, 'start')}>
                                Start Task
                              </button>
                            )}
                            {normalizeTaskStatus(t.status) === 'InProgress' && (
                              <button className="btn-secondary btn-sm" onClick={() => handleTaskAction(t.id, 'review')}>
                                Submit Review
                              </button>
                            )}
                          </div>
                        </div>
                      ))}
                  </div>
                )}
              </div>
            )}
          </div>
        )}

        {/* TAB 2: PERSONNEL & TEAMS */}
        {activeTab === 'employees' && (
          <div>
            <div className="page-header">
              <div>
                <h1 className="page-title">Personnel & Teams Directory</h1>
                <p className="page-subtitle">Organizational structure, department alignments, and talent management.</p>
              </div>
              <div className="header-actions">
                {currentUser?.role === 'Admin' && (
                  <>
                    <button className="btn-primary" onClick={() => setModalOpen('employee')}>
                      <Plus size={16} /> Add Employee
                    </button>
                    <button className="btn-secondary" onClick={() => setModalOpen('dept')}>
                      <Plus size={16} /> Add Department
                    </button>
                  </>
                )}
              </div>
            </div>

            {/* Department Cards */}
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem', flexWrap: 'wrap', gap: '0.75rem' }}>
              <h3 style={{ fontSize: '1.1rem', margin: 0 }}>
                Departments Overview ({filteredDepartments.length})
              </h3>
              <div style={{ display: 'flex', alignItems: 'center', gap: '1rem', flexWrap: 'wrap' }}>
                {testDeptCount > 0 && (
                  <label style={{ fontSize: '0.8rem', color: 'var(--text-muted)', display: 'inline-flex', alignItems: 'center', gap: '0.4rem', cursor: 'pointer' }}>
                    <input
                      type="checkbox"
                      checked={hideTestFixtures}
                      onChange={(e) => setHideTestFixtures(e.target.checked)}
                    />
                    Hide test fixtures ({testDeptCount} items)
                  </label>
                )}
                <input
                  type="text"
                  className="search-input"
                  style={{ maxWidth: '280px', padding: '0.4rem 0.75rem', fontSize: '0.85rem' }}
                  placeholder="Filter departments by name or manager..."
                  value={deptSearch}
                  onChange={(e) => setDeptSearch(e.target.value)}
                />
              </div>
            </div>

            <div className="dept-grid">
              {departments.length === 0 ? (
                <div className="glass-card" style={{ gridColumn: '1 / -1', padding: '2rem', textAlign: 'center', color: 'var(--text-muted)' }}>
                  No departments created yet.
                </div>
              ) : filteredDepartments.length === 0 ? (
                <div className="glass-card" style={{ gridColumn: '1 / -1', padding: '2rem', textAlign: 'center', color: 'var(--text-muted)' }}>
                  No departments matching &quot;{deptSearch}&quot;.
                </div>
              ) : (
                filteredDepartments.slice(0, 48).map((dept) => (
                  <div key={dept.id} className="glass-card dept-card" title={dept.name}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                      <span className="badge badge-inprogress">{(dept.name || 'DEP').slice(0, 3).toUpperCase()}</span>
                      <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>{dept.employeeCount ?? 0} Staff</span>
                    </div>
                    <h4 className="dept-title" title={dept.name}>{dept.name}</h4>
                    <p className="dept-manager" title={dept.managerName || 'Unassigned'}>
                      Manager: {dept.managerName || 'Unassigned'}
                    </p>
                  </div>
                ))
              )}
            </div>

            {/* Employee Directory */}
            <h3 style={{ marginBottom: '1rem', fontSize: '1.1rem' }}>Staff Directory</h3>
            <div className="filter-bar">
              <input
                type="text"
                className="search-input"
                placeholder="Search staff by name, department, or email..."
                value={employeeSearch}
                onChange={(e) => setEmployeeSearch(e.target.value)}
              />
              <select
                value={employeeDeptFilter}
                onChange={(e) => setEmployeeDeptFilter(e.target.value)}
              >
                <option value="">All Departments</option>
                {departments.map((d) => (
                  <option key={d.id} value={d.id}>{d.name}</option>
                ))}
              </select>
            </div>

            <div className="table-container">
              <table className="data-table">
                <thead>
                  <tr>
                    <th>Employee</th>
                    <th>Department</th>
                    <th>Hire Date</th>
                    <th>Leave Balance</th>
                    <th>Status</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredEmployees.length === 0 ? (
                    <tr>
                      <td colSpan="5" style={{ textAlign: 'center', padding: '2rem' }}>
                        No staff members matching your filter criteria.
                      </td>
                    </tr>
                  ) : (
                    filteredEmployees.map((emp) => (
                      <tr key={emp.id}>
                        <td>
                          <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
                            <div className="user-avatar" style={{ width: 34, height: 34, fontSize: '0.85rem' }}>
                              {(emp.firstName?.[0] || 'U')}{(emp.lastName?.[0] || '')}
                            </div>
                            <div>
                              <div style={{ fontWeight: 600 }}>{emp.firstName} {emp.lastName}</div>
                              <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>{emp.email}</div>
                            </div>
                          </div>
                        </td>
                        <td>{emp.departmentName || '—'}</td>
                        <td style={{ fontFamily: 'var(--font-mono)', fontSize: '0.85rem' }}>
                          {emp.hireDate?.split('T')[0]}
                        </td>
                        <td style={{ fontWeight: 600, color: '#34d399' }}>
                          {emp.annualLeaveBalanceDays} Days
                        </td>
                        <td>
                          <span className={`badge ${emp.isActive ? 'badge-approved' : 'badge-rejected'}`}>
                            {emp.isActive ? 'Active' : 'Inactive'}
                          </span>
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </div>
        )}

        {/* TAB 3: TIME OFF & LEAVES */}
        {activeTab === 'leaves' && (
          <div>
            <div className="page-header">
              <div>
                <h1 className="page-title">Leave Management</h1>
                <p className="page-subtitle">Submit paid time off, track available accruals, and process team absences.</p>
              </div>
              <div className="header-actions">
                <button className="btn-primary" onClick={() => setModalOpen('leave')}>
                  <Plus size={16} /> Request Time Off
                </button>
              </div>
            </div>

            {/* Sub Tabs */}
            <div style={{ display: 'flex', gap: '0.5rem', marginBottom: '1.5rem' }}>
              <button
                className={`tab-btn ${leaveSubTab === 'my' ? 'active' : ''}`}
                onClick={() => setLeaveSubTab('my')}
              >
                My Leave Requests ({myLeaves.length})
              </button>
              {(currentUser?.role === 'Admin' || currentUser?.role === 'Manager') && (
                <button
                  className={`tab-btn ${leaveSubTab === 'review' ? 'active' : ''}`}
                  onClick={() => setLeaveSubTab('review')}
                >
                  Pending Approvals Queue ({reviewLeaves.length})
                </button>
              )}
            </div>

            {leaveSubTab === 'my' ? (
              <div className="table-container">
                <table className="data-table">
                  <thead>
                    <tr>
                      <th>Dates</th>
                      <th>Days</th>
                      <th>Reason</th>
                      <th>Submitted</th>
                      <th>Status</th>
                      <th>Review Notes</th>
                      <th>Actions</th>
                    </tr>
                  </thead>
                  <tbody>
                    {myLeaves.length === 0 ? (
                      <tr><td colSpan="7" style={{ textAlign: 'center', padding: '2rem' }}>You have no submitted leave requests.</td></tr>
                    ) : (
                      myLeaves.map((lr) => (
                        <tr key={lr.id}>
                          <td style={{ fontFamily: 'var(--font-mono)', fontSize: '0.85rem' }}>
                            {(lr.startDate || '').split('T')[0] || lr.startDate || '—'} → {(lr.endDate || '').split('T')[0] || lr.endDate || '—'}
                          </td>
                          <td style={{ fontWeight: 700 }}>{lr.daysCount}</td>
                          <td>{lr.reason}</td>
                          <td style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>{(lr.createdAtUtc || '').split('T')[0] || lr.createdAtUtc || '—'}</td>
                          <td>
                            <span className={`badge badge-${formatLeaveStatus(lr.status).toLowerCase()}`}>{formatLeaveStatus(lr.status)}</span>
                          </td>
                          <td style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>{lr.rejectionReason || lr.reviewNotes || '—'}</td>
                          <td>
                            {formatLeaveStatus(lr.status) === 'Pending' && (
                              <button className="btn-danger btn-sm" onClick={() => handleCancelLeave(lr.id)}>
                                Cancel
                              </button>
                            )}
                          </td>
                        </tr>
                      ))
                    )}
                  </tbody>
                </table>
              </div>
            ) : (
              <div className="table-container">
                <table className="data-table">
                  <thead>
                    <tr>
                      <th>Employee</th>
                      <th>Department</th>
                      <th>Dates</th>
                      <th>Duration</th>
                      <th>Reason</th>
                      <th>Actions</th>
                    </tr>
                  </thead>
                  <tbody>
                    {reviewLeaves.length === 0 ? (
                      <tr><td colSpan="6" style={{ textAlign: 'center', padding: '2rem' }}>No pending leave requests requiring review.</td></tr>
                    ) : (
                      reviewLeaves.map((lr) => (
                        <tr key={lr.id}>
                          <td><strong>{lr.employeeName}</strong></td>
                          <td>{lr.departmentName}</td>
                          <td style={{ fontFamily: 'var(--font-mono)' }}>{(lr.startDate || '').split('T')[0] || lr.startDate || '—'} → {(lr.endDate || '').split('T')[0] || lr.endDate || '—'}</td>
                          <td><span className="badge badge-inprogress">{lr.daysCount} Days</span></td>
                          <td>{lr.reason}</td>
                          <td>
                            {lr.employeeId === personalData?.employeeId || (personalData?.fullName && lr.employeeName === personalData.fullName) ? (
                              <span className="badge badge-pending" title="Self-approval prohibited by internal controls">
                                Self-Submission
                              </span>
                            ) : (
                              <div style={{ display: 'flex', gap: '0.5rem' }}>
                                <button className="btn-success btn-sm" onClick={() => handleApproveLeave(lr.id)}>
                                  <CheckCircle2 size={14} /> Approve
                                </button>
                                <button
                                  className="btn-danger btn-sm"
                                  onClick={() => {
                                    setRejectTargetId(lr.id);
                                    setRejectionReason('');
                                    setModalError('');
                                    setModalOpen('rejectLeave');
                                  }}
                                >
                                  <XCircle size={14} /> Reject
                                </button>
                              </div>
                            )}
                          </td>
                        </tr>
                      ))
                    )}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        )}

        {/* TAB 4: TASK OPERATIONS */}
        {activeTab === 'tasks' && (
          <div>
            <div className="page-header">
              <div>
                <h1 className="page-title">Task Operations Matrix</h1>
                <p className="page-subtitle">Assign deliverables, monitor project progression, and execute state transitions.</p>
              </div>
              <div className="header-actions">
                <button className="btn-primary" onClick={() => setModalOpen('task')}>
                  <Plus size={16} /> Create Task
                </button>
              </div>
            </div>

            {/* Filter bar */}
            <div className="filter-bar">
              <input
                type="text"
                className="search-input"
                placeholder="Search tasks by title or details..."
                value={taskSearch}
                onChange={(e) => setTaskSearch(e.target.value)}
              />
              <select value={taskFilterStatus} onChange={(e) => setTaskFilterStatus(e.target.value)}>
                <option value="">All Statuses</option>
                <option value="Todo">Todo</option>
                <option value="InProgress">InProgress</option>
                <option value="InReview">InReview</option>
                <option value="Done">Done</option>
                <option value="Cancelled">Cancelled</option>
              </select>
              <select value={taskFilterPriority} onChange={(e) => setTaskFilterPriority(e.target.value)}>
                <option value="">All Priorities</option>
                <option value="0">Low</option>
                <option value="1">Medium</option>
                <option value="2">High</option>
                <option value="3">Urgent</option>
              </select>
            </div>

            {/* Kanban Columns */}
            <div className="kanban-board">
              {['Todo', 'InProgress', 'InReview', 'Done'].map((colStatus) => {
                const colTasks = tasks.filter((t) => normalizeTaskStatus(t.status) === colStatus);
                return (
                  <div key={colStatus} className="kanban-col">
                    <div className="kanban-col-header">
                      <span>{colStatus}</span>
                      <span className="badge badge-todo">{colTasks.length}</span>
                    </div>

                    {colTasks.length === 0 ? (
                      <div style={{ textAlign: 'center', padding: '1.5rem', color: 'var(--text-dim)', fontSize: '0.85rem' }}>
                        No tasks in {colStatus}
                      </div>
                    ) : (
                      colTasks.map((t) => {
                        const statusStr = normalizeTaskStatus(t.status);
                        const priorityStr = normalizeTaskPriority(t.priority);
                        return (
                          <div key={t.id} className="task-card">
                            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                              <span className={`badge badge-${priorityStr.toLowerCase()}`}>{priorityStr}</span>
                              <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>
                                Due: {t.dueDate?.split('T')[0] || 'No date'}
                              </span>
                            </div>
                            <h4 style={{ fontSize: '0.95rem', fontWeight: 600 }}>{t.title}</h4>
                            {t.description && (
                              <p style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>{t.description}</p>
                            )}
                            <div style={{ fontSize: '0.75rem', color: 'var(--text-dim)' }}>
                              Assignee: {t.assigneeName || 'Unassigned'}
                            </div>

                            {/* Lifecycle State Actions */}
                            <div className="task-actions">
                              {statusStr === 'Todo' && (
                                <button className="btn-secondary btn-sm" onClick={() => handleTaskAction(t.id, 'start')}>
                                  Start Task
                                </button>
                              )}
                              {statusStr === 'InProgress' && (
                                <button className="btn-secondary btn-sm" onClick={() => handleTaskAction(t.id, 'review')}>
                                  Submit Review
                                </button>
                              )}
                              {statusStr === 'InReview' && (currentUser?.role === 'Admin' || currentUser?.role === 'Manager') && (
                                <>
                                  <button className="btn-success btn-sm" onClick={() => handleTaskAction(t.id, 'complete')}>
                                    Complete
                                  </button>
                                  <button className="btn-secondary btn-sm" onClick={() => handleTaskAction(t.id, 'requestChanges')}>
                                    Req. Changes
                                  </button>
                                </>
                              )}
                              {statusStr !== 'Done' && statusStr !== 'Cancelled' && (currentUser?.role === 'Admin' || currentUser?.role === 'Manager') && (
                                <button
                                  className="btn-danger btn-sm"
                                  style={{ marginLeft: 'auto' }}
                                  onClick={() => handleTaskAction(t.id, 'cancel')}
                                >
                                  Cancel
                                </button>
                              )}
                            </div>
                          </div>
                        );
                      })
                    )}
                  </div>
                );
              })}
            </div>
          </div>
        )}

        {/* TAB 5: EXPENSE CLAIMS */}
        {activeTab === 'expenses' && (
          <div>
            <div className="page-header">
              <div>
                <h1 className="page-title">Expense Reimbursements</h1>
                <p className="page-subtitle">Submit corporate travel, equipment, and office expenses for approval.</p>
              </div>
              <div className="header-actions">
                <button className="btn-primary" onClick={() => setModalOpen('expense')}>
                  <Plus size={16} /> Claim Expense
                </button>
              </div>
            </div>

            {/* Sub Tabs */}
            <div style={{ display: 'flex', gap: '0.5rem', marginBottom: '1.5rem' }}>
              <button
                className={`tab-btn ${expenseSubTab === 'my' ? 'active' : ''}`}
                onClick={() => setExpenseSubTab('my')}
              >
                My Claims ({myExpenses.length})
              </button>
              {(currentUser?.role === 'Admin' || currentUser?.role === 'Manager') && (
                <button
                  className={`tab-btn ${expenseSubTab === 'review' ? 'active' : ''}`}
                  onClick={() => setExpenseSubTab('review')}
                >
                  Pending Approvals Queue ({reviewExpenses.length})
                </button>
              )}
            </div>

            {expenseSubTab === 'my' ? (
              <div className="table-container">
                <table className="data-table">
                  <thead>
                    <tr>
                      <th>Date</th>
                      <th>Category</th>
                      <th>Amount</th>
                      <th>Description</th>
                      <th>Status</th>
                      <th>Review Notes</th>
                      <th>Actions</th>
                    </tr>
                  </thead>
                  <tbody>
                    {myExpenses.length === 0 ? (
                      <tr><td colSpan="7" style={{ textAlign: 'center', padding: '2rem' }}>No expenses submitted yet.</td></tr>
                    ) : (
                      myExpenses.map((exp) => (
                        <tr key={exp.id}>
                          <td style={{ fontFamily: 'var(--font-mono)' }}>{(exp.expenseDate || '').split('T')[0] || exp.expenseDate || '—'}</td>
                          <td><span className="badge badge-inprogress">{formatExpenseCategory(exp.category)}</span></td>
                          <td style={{ fontWeight: 700 }}>${Number(exp.amount || 0).toFixed(2)} {exp.currency}</td>
                          <td>{exp.description}</td>
                          <td><span className={`badge badge-${formatExpenseStatus(exp.status).toLowerCase()}`}>{formatExpenseStatus(exp.status)}</span></td>
                          <td style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>{exp.rejectionReason || exp.reviewNotes || '—'}</td>
                          <td>
                            {formatExpenseStatus(exp.status) === 'Pending' && (
                              <button className="btn-danger btn-sm" onClick={() => handleCancelExpense(exp.id)}>
                                Cancel
                              </button>
                            )}
                          </td>
                        </tr>
                      ))
                    )}
                  </tbody>
                </table>
              </div>
            ) : (
              <div className="table-container">
                <table className="data-table">
                  <thead>
                    <tr>
                      <th>Employee</th>
                      <th>Department</th>
                      <th>Category</th>
                      <th>Amount</th>
                      <th>Description</th>
                      <th>Date</th>
                      <th>Actions</th>
                    </tr>
                  </thead>
                  <tbody>
                    {reviewExpenses.length === 0 ? (
                      <tr><td colSpan="7" style={{ textAlign: 'center', padding: '2rem' }}>No pending expenses awaiting approval.</td></tr>
                    ) : (
                      reviewExpenses.map((exp) => (
                        <tr key={exp.id}>
                          <td><strong>{exp.employeeName}</strong></td>
                          <td>{exp.departmentName}</td>
                          <td><span className="badge badge-inprogress">{formatExpenseCategory(exp.category)}</span></td>
                          <td style={{ fontWeight: 700, color: '#34d399' }}>${Number(exp.amount || 0).toFixed(2)} {exp.currency}</td>
                          <td>{exp.description}</td>
                          <td style={{ fontFamily: 'var(--font-mono)' }}>{(exp.expenseDate || '').split('T')[0] || exp.expenseDate || '—'}</td>
                          <td>
                            {exp.employeeId === personalData?.employeeId || (personalData?.fullName && exp.employeeName === personalData.fullName) ? (
                              <span className="badge badge-pending" title="Self-approval prohibited by internal controls">
                                Self-Submission
                              </span>
                            ) : (
                              <div style={{ display: 'flex', gap: '0.5rem' }}>
                                <button className="btn-success btn-sm" onClick={() => handleApproveExpense(exp.id)}>
                                  <CheckCircle2 size={14} /> Approve
                                </button>
                                <button
                                  className="btn-danger btn-sm"
                                  onClick={() => {
                                    setRejectTargetId(exp.id);
                                    setRejectionReason('');
                                    setModalError('');
                                    setModalOpen('rejectExpense');
                                  }}
                                >
                                  <XCircle size={14} /> Reject
                                </button>
                              </div>
                            )}
                          </td>
                        </tr>
                      ))
                    )}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        )}
      </div>

      {/* AUTHENTICATION / QUICK ROLE MODAL */}
      {showAuthModal && (
        <div className="modal-overlay" onClick={() => setShowAuthModal(false)}>
          <div className="modal-card" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h3 className="modal-title">{authMode === 'login' ? 'Sign In to Operix' : 'Create New Account'}</h3>
              <button className="btn-secondary btn-sm" onClick={() => setShowAuthModal(false)}>✕</button>
            </div>

            {/* Quick Demo Switcher */}
            <p style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginBottom: '0.5rem' }}>
              Select Demo Credentials to Populate Form (Click "Sign In" below to authenticate):
            </p>
            <div className="demo-accounts">
              <div
                className="demo-account-card"
                role="button"
                tabIndex={0}
                onClick={() => fillDemoCredentials('admin@operix.com', 'Admin123!')}
                onKeyDown={(e) => e.key === 'Enter' && fillDemoCredentials('admin@operix.com', 'Admin123!')}
                title="Click to populate Admin credentials"
              >
                <ShieldCheck size={20} color="#818cf8" />
                <span className="demo-role">Admin</span>
                <span className="demo-cred">admin@operix.com</span>
                <span className="demo-hint-badge">Load Admin</span>
              </div>
              <div
                className="demo-account-card"
                role="button"
                tabIndex={0}
                onClick={() => fillDemoCredentials('manager@operix.com', 'Manager123!')}
                onKeyDown={(e) => e.key === 'Enter' && fillDemoCredentials('manager@operix.com', 'Manager123!')}
                title="Click to populate Manager credentials"
              >
                <UserCheck size={20} color="#22d3ee" />
                <span className="demo-role">Manager</span>
                <span className="demo-cred">manager@operix.com</span>
                <span className="demo-hint-badge">Load Manager</span>
              </div>
              <div
                className="demo-account-card"
                role="button"
                tabIndex={0}
                onClick={() => fillDemoCredentials('employee@operix.com', 'Employee123!')}
                onKeyDown={(e) => e.key === 'Enter' && fillDemoCredentials('employee@operix.com', 'Employee123!')}
                title="Click to populate Employee credentials"
              >
                <Users size={20} color="#34d399" />
                <span className="demo-role">Employee</span>
                <span className="demo-cred">employee@operix.com</span>
                <span className="demo-hint-badge">Load Employee</span>
              </div>
            </div>

            {authError && (
              <div className="form-banner-error" role="alert">
                <AlertCircle size={16} />
                <span>{authError}</span>
              </div>
            )}

            {/* Form */}
            <form onSubmit={authMode === 'login' ? handleLogin : handleRegister} noValidate>
              {authMode === 'login' ? (
                <>
                  <div className="form-group">
                    <label>Email Address or Username</label>
                    <input
                      type="text"
                      required
                      value={authForm.email}
                      onChange={(e) => {
                        setAuthForm({ ...authForm, email: e.target.value });
                        setAuthFieldErrors((prev) => ({ ...prev, email: null }));
                      }}
                      placeholder="e.g. admin@operix.com or admin"
                      autoComplete="username"
                    />
                    {authFieldErrors.email && (
                      <span className="field-error-msg">
                        <AlertCircle size={12} /> {authFieldErrors.email}
                      </span>
                    )}
                  </div>
                  <div className="form-group">
                    <label>Password</label>
                    <div className="password-input-wrapper">
                      <input
                        type={showPassword ? 'text' : 'password'}
                        required
                        value={authForm.password}
                        onChange={(e) => {
                          setAuthForm({ ...authForm, password: e.target.value });
                          setAuthFieldErrors((prev) => ({ ...prev, password: null }));
                        }}
                        placeholder="••••••••"
                        autoComplete="current-password"
                      />
                      <button
                        type="button"
                        className="password-toggle-btn"
                        onClick={() => setShowPassword(!showPassword)}
                        aria-label={showPassword ? 'Hide password' : 'Show password'}
                        title={showPassword ? 'Hide password' : 'Show password'}
                      >
                        {showPassword ? <EyeOff size={16} /> : <Eye size={16} />}
                      </button>
                    </div>
                    {authFieldErrors.password && (
                      <span className="field-error-msg">
                        <AlertCircle size={12} /> {authFieldErrors.password}
                      </span>
                    )}
                  </div>
                </>
              ) : (
                <>
                  <div className="form-row">
                    <div className="form-group">
                      <label>First Name</label>
                      <input
                        type="text"
                        required
                        value={authForm.firstName}
                        onChange={(e) => {
                          setAuthForm({ ...authForm, firstName: e.target.value });
                          setAuthFieldErrors((prev) => ({ ...prev, firstName: null }));
                        }}
                        placeholder="e.g. Jane"
                      />
                      {authFieldErrors.firstName && (
                        <span className="field-error-msg">
                          <AlertCircle size={12} /> {authFieldErrors.firstName}
                        </span>
                      )}
                    </div>
                    <div className="form-group">
                      <label>Last Name</label>
                      <input
                        type="text"
                        required
                        value={authForm.lastName}
                        onChange={(e) => {
                          setAuthForm({ ...authForm, lastName: e.target.value });
                          setAuthFieldErrors((prev) => ({ ...prev, lastName: null }));
                        }}
                        placeholder="e.g. Doe"
                      />
                      {authFieldErrors.lastName && (
                        <span className="field-error-msg">
                          <AlertCircle size={12} /> {authFieldErrors.lastName}
                        </span>
                      )}
                    </div>
                  </div>
                  <div className="form-group">
                    <label>Email Address</label>
                    <input
                      type="email"
                      required
                      value={authForm.email}
                      onChange={(e) => {
                        setAuthForm({ ...authForm, email: e.target.value });
                        setAuthFieldErrors((prev) => ({ ...prev, email: null }));
                      }}
                      placeholder="e.g. user@company.com"
                      autoComplete="email"
                    />
                    {authFieldErrors.email && (
                      <span className="field-error-msg">
                        <AlertCircle size={12} /> {authFieldErrors.email}
                      </span>
                    )}
                  </div>
                  <div className="form-group">
                    <label>Account Role</label>
                    <select
                      value={authForm.role}
                      onChange={(e) => setAuthForm({ ...authForm, role: e.target.value })}
                    >
                      <option value="Employee">Employee (Staff Member)</option>
                      <option value="Manager">Manager (Team Reviewer)</option>
                      <option value="Admin">Admin (System Administrator)</option>
                    </select>
                  </div>
                  <div className="form-group">
                    <label>Password</label>
                    <div className="password-input-wrapper">
                      <input
                        type={showPassword ? 'text' : 'password'}
                        required
                        value={authForm.password}
                        onChange={(e) => {
                          setAuthForm({ ...authForm, password: e.target.value });
                          setAuthFieldErrors((prev) => ({ ...prev, password: null }));
                        }}
                        placeholder="Min 8 chars (e.g. SecurePass123!)"
                        autoComplete="new-password"
                      />
                      <button
                        type="button"
                        className="password-toggle-btn"
                        onClick={() => setShowPassword(!showPassword)}
                        aria-label={showPassword ? 'Hide password' : 'Show password'}
                        title={showPassword ? 'Hide password' : 'Show password'}
                      >
                        {showPassword ? <EyeOff size={16} /> : <Eye size={16} />}
                      </button>
                    </div>
                    {authFieldErrors.password && (
                      <span className="field-error-msg">
                        <AlertCircle size={12} /> {authFieldErrors.password}
                      </span>
                    )}
                    <span style={{ fontSize: '0.72rem', color: 'var(--text-dim)', marginTop: '0.2rem' }}>
                      Requirements: 8+ characters, with uppercase, lowercase, number, and symbol (!?*@#$%^&+=).
                    </span>
                  </div>
                </>
              )}

              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: '1.5rem' }}>
                <span
                  style={{ fontSize: '0.85rem', color: 'var(--primary)', cursor: 'pointer' }}
                  onClick={() => {
                    setAuthMode(authMode === 'login' ? 'register' : 'login');
                    setAuthError('');
                    setAuthFieldErrors({});
                  }}
                >
                  {authMode === 'login' ? 'Need an account? Register' : 'Already have an account? Sign In'}
                </span>
                <button type="submit" className="btn-primary" disabled={isSubmitting}>
                  {isSubmitting && <span className="btn-spinner" />}
                  {authMode === 'login' ? (isSubmitting ? 'Signing In...' : 'Sign In') : (isSubmitting ? 'Registering...' : 'Register Account')}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* MODAL: ADD DEPARTMENT */}
      {modalOpen === 'dept' && (
        <div className="modal-overlay" onClick={() => setModalOpen(null)}>
          <div className="modal-card" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h3 className="modal-title">Create New Department</h3>
              <button className="btn-secondary btn-sm" onClick={() => setModalOpen(null)}>✕</button>
            </div>
            {modalError && (
              <div className="form-banner-error" role="alert">
                <AlertCircle size={16} />
                <span>{modalError}</span>
              </div>
            )}
            <form onSubmit={handleCreateDepartment}>
              <div className="form-group">
                <label>Department Name</label>
                <input
                  type="text"
                  required
                  placeholder="e.g. Global Operations"
                  value={newDeptForm.name}
                  onChange={(e) => setNewDeptForm({ ...newDeptForm, name: e.target.value })}
                />
              </div>
              <div className="form-group">
                <label>Description</label>
                <textarea
                  rows={3}
                  placeholder="Core department mission..."
                  value={newDeptForm.description}
                  onChange={(e) => setNewDeptForm({ ...newDeptForm, description: e.target.value })}
                />
              </div>
              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem', marginTop: '1.5rem' }}>
                <button type="button" className="btn-secondary" onClick={() => setModalOpen(null)}>Cancel</button>
                <button type="submit" className="btn-primary" disabled={isSubmitting}>
                  {isSubmitting && <span className="btn-spinner" />}
                  {isSubmitting ? 'Creating...' : 'Create Department'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* MODAL: ADD EMPLOYEE */}
      {modalOpen === 'employee' && (
        <div className="modal-overlay" onClick={() => setModalOpen(null)}>
          <div className="modal-card" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h3 className="modal-title">Create Employee Profile</h3>
              <button className="btn-secondary btn-sm" onClick={() => setModalOpen(null)}>✕</button>
            </div>
            {modalError && (
              <div className="form-banner-error" role="alert">
                <AlertCircle size={16} />
                <span>{modalError}</span>
              </div>
            )}
            <form onSubmit={handleCreateEmployee}>
              <div className="form-row">
                <div className="form-group">
                  <label>First Name</label>
                  <input
                    type="text"
                    required
                    value={newEmployeeForm.firstName}
                    onChange={(e) => setNewEmployeeForm({ ...newEmployeeForm, firstName: e.target.value })}
                  />
                </div>
                <div className="form-group">
                  <label>Last Name</label>
                  <input
                    type="text"
                    required
                    value={newEmployeeForm.lastName}
                    onChange={(e) => setNewEmployeeForm({ ...newEmployeeForm, lastName: e.target.value })}
                  />
                </div>
              </div>
              <div className="form-group">
                <label>Email Address</label>
                <input
                  type="email"
                  required
                  placeholder="e.g. employee@company.com"
                  value={newEmployeeForm.email}
                  onChange={(e) => setNewEmployeeForm({ ...newEmployeeForm, email: e.target.value })}
                />
              </div>
              <div className="form-row">
                <div className="form-group">
                  <label>Department</label>
                  <select
                    required
                    value={newEmployeeForm.departmentId}
                    onChange={(e) => setNewEmployeeForm({ ...newEmployeeForm, departmentId: e.target.value })}
                  >
                    <option value="">Select Department...</option>
                    {departments.map((d) => (
                      <option key={d.id} value={d.id}>{d.name}</option>
                    ))}
                  </select>
                </div>
                <div className="form-group">
                  <label>Reporting Manager (Optional)</label>
                  <select
                    value={newEmployeeForm.managerId}
                    onChange={(e) => setNewEmployeeForm({ ...newEmployeeForm, managerId: e.target.value })}
                  >
                    <option value="">No Manager Assigned</option>
                    {employees.map((emp) => (
                      <option key={emp.id} value={emp.id}>{emp.firstName} {emp.lastName}</option>
                    ))}
                  </select>
                </div>
              </div>
              <div className="form-row">
                <div className="form-group">
                  <label>Hire Date</label>
                  <input
                    type="date"
                    required
                    value={newEmployeeForm.hireDate}
                    onChange={(e) => setNewEmployeeForm({ ...newEmployeeForm, hireDate: e.target.value })}
                  />
                </div>
                <div className="form-group">
                  <label>Annual Leave Balance Days</label>
                  <input
                    type="number"
                    min="0"
                    required
                    value={newEmployeeForm.annualLeaveBalanceDays}
                    onChange={(e) => setNewEmployeeForm({ ...newEmployeeForm, annualLeaveBalanceDays: e.target.value })}
                  />
                </div>
              </div>
              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem', marginTop: '1.5rem' }}>
                <button type="button" className="btn-secondary" onClick={() => setModalOpen(null)}>Cancel</button>
                <button type="submit" className="btn-primary" disabled={isSubmitting}>
                  {isSubmitting && <span className="btn-spinner" />}
                  {isSubmitting ? 'Saving...' : 'Save Profile'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* MODAL: APPLY TIME OFF */}
      {modalOpen === 'leave' && (
        <div className="modal-overlay" onClick={() => setModalOpen(null)}>
          <div className="modal-card" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h3 className="modal-title">Request Time Off</h3>
              <button className="btn-secondary btn-sm" onClick={() => setModalOpen(null)}>✕</button>
            </div>
            {modalError && (
              <div className="form-banner-error" role="alert">
                <AlertCircle size={16} />
                <span>{modalError}</span>
              </div>
            )}
            <form onSubmit={handleApplyLeave}>
              <div className="form-row">
                <div className="form-group">
                  <label>Start Date</label>
                  <input
                    type="date"
                    required
                    value={newLeaveForm.startDate}
                    onChange={(e) => setNewLeaveForm({ ...newLeaveForm, startDate: e.target.value })}
                  />
                </div>
                <div className="form-group">
                  <label>End Date</label>
                  <input
                    type="date"
                    required
                    value={newLeaveForm.endDate}
                    onChange={(e) => setNewLeaveForm({ ...newLeaveForm, endDate: e.target.value })}
                  />
                </div>
              </div>
              <div className="form-group">
                <label>Reason for Leave</label>
                <textarea
                  rows={3}
                  required
                  placeholder="e.g. Annual family vacation..."
                  value={newLeaveForm.reason}
                  onChange={(e) => setNewLeaveForm({ ...newLeaveForm, reason: e.target.value })}
                />
              </div>
              {isLeaveDateConflicted(newLeaveForm.startDate, newLeaveForm.endDate) && (
                <div className="form-banner-error" role="alert" style={{ marginBottom: '1rem' }}>
                  <AlertCircle size={16} />
                  <span>These dates overlap with an active or pending leave request in your records.</span>
                </div>
              )}
              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem', marginTop: '1.5rem' }}>
                <button type="button" className="btn-secondary" onClick={() => setModalOpen(null)}>Cancel</button>
                <button
                  type="submit"
                  className="btn-primary"
                  disabled={isSubmitting || isLeaveDateConflicted(newLeaveForm.startDate, newLeaveForm.endDate)}
                >
                  {isSubmitting && <span className="btn-spinner" />}
                  {isSubmitting ? 'Submitting...' : 'Submit Request'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* MODAL: CREATE TASK */}
      {modalOpen === 'task' && (
        <div className="modal-overlay" onClick={() => setModalOpen(null)}>
          <div className="modal-card" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h3 className="modal-title">Create Operational Task</h3>
              <button className="btn-secondary btn-sm" onClick={() => setModalOpen(null)}>✕</button>
            </div>
            {modalError && (
              <div className="form-banner-error" role="alert">
                <AlertCircle size={16} />
                <span>{modalError}</span>
              </div>
            )}
            <form onSubmit={handleCreateTask}>
              <div className="form-group">
                <label>Task Title</label>
                <input
                  type="text"
                  required
                  placeholder="e.g. Finalize Q4 Financial Audit"
                  value={newTaskForm.title}
                  onChange={(e) => setNewTaskForm({ ...newTaskForm, title: e.target.value })}
                />
              </div>
              <div className="form-group">
                <label>Description</label>
                <textarea
                  rows={3}
                  placeholder="Deliverables and key objectives..."
                  value={newTaskForm.description}
                  onChange={(e) => setNewTaskForm({ ...newTaskForm, description: e.target.value })}
                />
              </div>
              <div className="form-row">
                <div className="form-group">
                  <label>Priority</label>
                  <select
                    value={newTaskForm.priority}
                    onChange={(e) => setNewTaskForm({ ...newTaskForm, priority: e.target.value })}
                  >
                    <option value="0">Low</option>
                    <option value="1">Medium</option>
                    <option value="2">High</option>
                    <option value="3">Urgent</option>
                  </select>
                </div>
                <div className="form-group">
                  <label>Due Date</label>
                  <input
                    type="date"
                    value={newTaskForm.dueDate}
                    onChange={(e) => setNewTaskForm({ ...newTaskForm, dueDate: e.target.value })}
                  />
                </div>
              </div>
              <div className="form-group">
                <label>Assign to Team Member</label>
                <select
                  value={newTaskForm.assignedToEmployeeId}
                  onChange={(e) => setNewTaskForm({ ...newTaskForm, assignedToEmployeeId: e.target.value })}
                >
                  <option value="">Unassigned</option>
                  {assignableEmployees.map((emp) => (
                    <option key={emp.id} value={emp.id}>{emp.firstName} {emp.lastName} ({emp.departmentName || 'Staff'})</option>
                  ))}
                </select>
              </div>
              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem', marginTop: '1.5rem' }}>
                <button type="button" className="btn-secondary" onClick={() => setModalOpen(null)}>Cancel</button>
                <button type="submit" className="btn-primary" disabled={isSubmitting}>
                  {isSubmitting && <span className="btn-spinner" />}
                  {isSubmitting ? 'Creating...' : 'Create Task'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* MODAL: CLAIM EXPENSE */}
      {modalOpen === 'expense' && (
        <div className="modal-overlay" onClick={() => setModalOpen(null)}>
          <div className="modal-card" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h3 className="modal-title">Submit Expense Reimbursement</h3>
              <button className="btn-secondary btn-sm" onClick={() => setModalOpen(null)}>✕</button>
            </div>
            {modalError && (
              <div className="form-banner-error" role="alert">
                <AlertCircle size={16} />
                <span>{modalError}</span>
              </div>
            )}
            <form onSubmit={handleSubmitExpense}>
              <div className="form-row">
                <div className="form-group">
                  <label>Amount ($)</label>
                  <input
                    type="number"
                    step="0.01"
                    min="0.01"
                    required
                    value={newExpenseForm.amount}
                    onChange={(e) => setNewExpenseForm({ ...newExpenseForm, amount: e.target.value })}
                  />
                </div>
                <div className="form-group">
                  <label>Currency</label>
                  <select
                    value={newExpenseForm.currency}
                    onChange={(e) => setNewExpenseForm({ ...newExpenseForm, currency: e.target.value })}
                  >
                    <option value="USD">USD ($)</option>
                    <option value="EUR">EUR (€)</option>
                    <option value="GBP">GBP (£)</option>
                  </select>
                </div>
              </div>
              <div className="form-row">
                <div className="form-group">
                  <label>Category</label>
                  <select
                    value={newExpenseForm.category}
                    onChange={(e) => setNewExpenseForm({ ...newExpenseForm, category: e.target.value })}
                  >
                    <option value="1">Travel</option>
                    <option value="2">Meals</option>
                    <option value="3">Equipment</option>
                    <option value="4">Office Supplies</option>
                    <option value="5">Training</option>
                    <option value="6">Other</option>
                  </select>
                </div>
                <div className="form-group">
                  <label>Expense Date</label>
                  <input
                    type="date"
                    required
                    value={newExpenseForm.expenseDate}
                    onChange={(e) => setNewExpenseForm({ ...newExpenseForm, expenseDate: e.target.value })}
                  />
                </div>
              </div>
              <div className="form-group">
                <label>Description / Business Justification</label>
                <textarea
                  rows={3}
                  required
                  placeholder="e.g. Flight ticket for enterprise client kick-off meeting..."
                  value={newExpenseForm.description}
                  onChange={(e) => setNewExpenseForm({ ...newExpenseForm, description: e.target.value })}
                />
              </div>
              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem', marginTop: '1.5rem' }}>
                <button type="button" className="btn-secondary" onClick={() => setModalOpen(null)}>Cancel</button>
                <button type="submit" className="btn-primary" disabled={isSubmitting}>
                  {isSubmitting && <span className="btn-spinner" />}
                  {isSubmitting ? 'Submitting...' : 'Submit Claim'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* MODAL: REJECT LEAVE WITH REASON */}
      {modalOpen === 'rejectLeave' && (
        <div className="modal-overlay" onClick={() => setModalOpen(null)}>
          <div className="modal-card" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h3 className="modal-title">Reject Leave Request</h3>
              <button className="btn-secondary btn-sm" onClick={() => setModalOpen(null)}>✕</button>
            </div>
            {modalError && (
              <div className="form-banner-error" role="alert">
                <AlertCircle size={16} />
                <span>{modalError}</span>
              </div>
            )}
            <div className="form-group">
              <label>Rejection Reason (Required)</label>
              <textarea
                rows={3}
                required
                placeholder="Explain reason for rejection..."
                value={rejectionReason}
                onChange={(e) => setRejectionReason(e.target.value)}
              />
            </div>
            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem', marginTop: '1.5rem' }}>
              <button type="button" className="btn-secondary" onClick={() => setModalOpen(null)}>Cancel</button>
              <button type="button" className="btn-danger" onClick={handleRejectLeaveConfirm} disabled={isSubmitting}>
                {isSubmitting && <span className="btn-spinner" />}
                {isSubmitting ? 'Rejecting...' : 'Confirm Rejection'}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* MODAL: REJECT EXPENSE WITH REASON */}
      {modalOpen === 'rejectExpense' && (
        <div className="modal-overlay" onClick={() => setModalOpen(null)}>
          <div className="modal-card" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h3 className="modal-title">Reject Expense Reimbursement</h3>
              <button className="btn-secondary btn-sm" onClick={() => setModalOpen(null)}>✕</button>
            </div>
            {modalError && (
              <div className="form-banner-error" role="alert">
                <AlertCircle size={16} />
                <span>{modalError}</span>
              </div>
            )}
            <div className="form-group">
              <label>Rejection Reason (Required)</label>
              <textarea
                rows={3}
                required
                placeholder="Explain reason for rejection..."
                value={rejectionReason}
                onChange={(e) => setRejectionReason(e.target.value)}
              />
            </div>
            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem', marginTop: '1.5rem' }}>
              <button type="button" className="btn-secondary" onClick={() => setModalOpen(null)}>Cancel</button>
              <button type="button" className="btn-danger" onClick={handleRejectExpenseConfirm} disabled={isSubmitting}>
                {isSubmitting && <span className="btn-spinner" />}
                {isSubmitting ? 'Rejecting...' : 'Confirm Rejection'}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* FLOATING TOASTS */}
      <div className="toast-container">
        {toasts.map((t) => (
          <div key={t.id} className={`toast ${t.type}`}>
            {t.type === 'success' ? <CheckCircle2 size={18} /> : <AlertCircle size={18} />}
            <span>{t.message}</span>
          </div>
        ))}
      </div>
    </div>
  );
}
