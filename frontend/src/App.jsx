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
  ArrowRight
} from 'lucide-react';

export default function App() {
  const [currentUser, setCurrentUser] = useState(getSavedUser());
  const [activeTab, setActiveTab] = useState('dashboard');
  const [healthStatus, setHealthStatus] = useState({ online: false, db: 'Unknown', latency: null });
  const [toasts, setToasts] = useState([]);

  // Auth modal
  const [showAuthModal, setShowAuthModal] = useState(false);
  const [authMode, setAuthMode] = useState('login'); // 'login' or 'register'
  const [authForm, setAuthForm] = useState({ username: '', email: '', password: '', role: 'Employee' });

  // Dashboard Data
  const [summaryData, setSummaryData] = useState(null);
  const [personalData, setPersonalData] = useState(null);

  // Departments & Employees
  const [departments, setDepartments] = useState([]);
  const [employees, setEmployees] = useState([]);
  const [employeeSearch, setEmployeeSearch] = useState('');
  const [employeeDeptFilter, setEmployeeDeptFilter] = useState('');

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

  // Forms
  const [newDeptForm, setNewDeptForm] = useState({ name: '', code: '', description: '' });
  const [newEmployeeForm, setNewEmployeeForm] = useState({
    userId: '',
    firstName: '',
    lastName: '',
    departmentId: '',
    jobTitle: '',
    salary: 65000,
    annualLeaveBalanceDays: 20
  });
  const [newLeaveForm, setNewLeaveForm] = useState({
    startDate: new Date().toISOString().split('T')[0],
    endDate: new Date(Date.now() + 86400000 * 3).toISOString().split('T')[0],
    reason: ''
  });
  const [newTaskForm, setNewTaskForm] = useState({
    title: '',
    description: '',
    priority: 1, // Medium
    assignedToEmployeeId: '',
    dueDate: new Date(Date.now() + 86400000 * 7).toISOString().split('T')[0]
  });
  const [newExpenseForm, setNewExpenseForm] = useState({
    amount: 50.0,
    currency: 'USD',
    category: 0, // Travel
    expenseDate: new Date().toISOString().split('T')[0],
    description: ''
  });

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

  // Fetch initial core data
  useEffect(() => {
    if (currentUser) {
      loadUserData();
    }
  }, [currentUser]);

  const loadUserData = async () => {
    try {
      if (currentUser?.role === 'Admin' || currentUser?.role === 'Manager') {
        api.dashboard.summary().then(setSummaryData).catch(() => {});
      }
      api.dashboard.my().then(setPersonalData).catch(() => {});
      api.departments.list().then(setDepartments).catch(() => {});
      api.employees.list({ pageSize: 50 }).then((res) => setEmployees(res.items || [])).catch(() => {});
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
      }
    } catch (e) { console.error(e); }
  };

  const loadTasks = async () => {
    try {
      const res = await api.tasks.list({
        pageSize: 50,
        status: taskFilterStatus || undefined,
        priority: taskFilterPriority || undefined,
        search: taskSearch || undefined
      });
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
      }
    } catch (e) { console.error(e); }
  };

  useEffect(() => {
    if (currentUser) {
      loadTasks();
    }
  }, [taskFilterStatus, taskFilterPriority, taskSearch]);

  // Auth Actions
  const handleLogin = async (e, demoCreds = null) => {
    if (e) e.preventDefault();
    const identifier = demoCreds ? demoCreds.username : authForm.username;
    const password = demoCreds ? demoCreds.password : authForm.password;

    try {
      const res = await api.auth.login(identifier, password);
      setAuthToken(res.token);
      setSavedUser(res.user);
      setCurrentUser(res.user);
      setShowAuthModal(false);
      showToast(`Welcome back, ${res.user.username} (${res.user.role})!`);
    } catch (err) {
      showToast(err.message, 'error');
    }
  };

  const handleRegister = async (e) => {
    e.preventDefault();
    try {
      const res = await api.auth.register({
        username: authForm.username,
        email: authForm.email,
        password: authForm.password,
        role: authForm.role
      });
      setAuthToken(res.token);
      setSavedUser(res.user);
      setCurrentUser(res.user);
      setShowAuthModal(false);
      showToast(`Account registered successfully as ${res.user.role}!`);
    } catch (err) {
      showToast(err.message, 'error');
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
    try {
      await api.departments.create(newDeptForm);
      showToast('Department created successfully!');
      setModalOpen(null);
      setNewDeptForm({ name: '', code: '', description: '' });
      api.departments.list().then(setDepartments);
    } catch (err) {
      showToast(err.message, 'error');
    }
  };

  const handleCreateEmployee = async (e) => {
    e.preventDefault();
    try {
      await api.employees.create({
        ...newEmployeeForm,
        userId: newEmployeeForm.userId || currentUser.id,
        salary: Number(newEmployeeForm.salary),
        annualLeaveBalanceDays: Number(newEmployeeForm.annualLeaveBalanceDays)
      });
      showToast('Employee profile added!');
      setModalOpen(null);
      api.employees.list({ pageSize: 50 }).then((res) => setEmployees(res.items || []));
    } catch (err) {
      showToast(err.message, 'error');
    }
  };

  const handleApplyLeave = async (e) => {
    e.preventDefault();
    try {
      await api.leave.submit(newLeaveForm);
      showToast('Time-off request submitted!');
      setModalOpen(null);
      loadLeaves();
      api.dashboard.my().then(setPersonalData);
    } catch (err) {
      showToast(err.message, 'error');
    }
  };

  const handleApproveLeave = async (id) => {
    try {
      await api.leave.approve(id);
      showToast('Leave request approved! Balance updated.');
      loadLeaves();
      if (summaryData) api.dashboard.summary().then(setSummaryData);
    } catch (err) {
      showToast(err.message, 'error');
    }
  };

  const handleRejectLeaveConfirm = async () => {
    try {
      if (!rejectionReason.trim()) {
        showToast('A rejection reason is required', 'error');
        return;
      }
      await api.leave.reject(rejectTargetId, rejectionReason);
      showToast('Leave request rejected');
      setModalOpen(null);
      setRejectionReason('');
      loadLeaves();
    } catch (err) {
      showToast(err.message, 'error');
    }
  };

  const handleCancelLeave = async (id) => {
    try {
      await api.leave.cancel(id);
      showToast('Leave request cancelled');
      loadLeaves();
    } catch (err) {
      showToast(err.message, 'error');
    }
  };

  const handleCreateTask = async (e) => {
    e.preventDefault();
    try {
      await api.tasks.create({
        ...newTaskForm,
        priority: Number(newTaskForm.priority),
        assignedToEmployeeId: newTaskForm.assignedToEmployeeId || undefined
      });
      showToast('Task created successfully!');
      setModalOpen(null);
      loadTasks();
    } catch (err) {
      showToast(err.message, 'error');
    }
  };

  const handleTaskAction = async (taskId, action) => {
    try {
      if (action === 'start') await api.tasks.start(taskId);
      else if (action === 'review') await api.tasks.review(taskId);
      else if (action === 'requestChanges') await api.tasks.requestChanges(taskId);
      else if (action === 'complete') await api.tasks.complete(taskId);
      else if (action === 'cancel') await api.tasks.cancel(taskId);
      showToast(`Task status updated!`);
      loadTasks();
    } catch (err) {
      showToast(err.message, 'error');
    }
  };

  const handleSubmitExpense = async (e) => {
    e.preventDefault();
    try {
      await api.expenses.submit({
        ...newExpenseForm,
        amount: Number(newExpenseForm.amount),
        category: Number(newExpenseForm.category)
      });
      showToast('Expense reimbursement submitted!');
      setModalOpen(null);
      loadExpenses();
    } catch (err) {
      showToast(err.message, 'error');
    }
  };

  const handleApproveExpense = async (id) => {
    try {
      await api.expenses.approve(id);
      showToast('Expense approved for reimbursement!');
      loadExpenses();
    } catch (err) {
      showToast(err.message, 'error');
    }
  };

  const handleRejectExpenseConfirm = async () => {
    try {
      if (!rejectionReason.trim()) {
        showToast('A rejection reason is required', 'error');
        return;
      }
      await api.expenses.reject(rejectTargetId, rejectionReason);
      showToast('Expense reimbursement rejected');
      setModalOpen(null);
      setRejectionReason('');
      loadExpenses();
    } catch (err) {
      showToast(err.message, 'error');
    }
  };

  const handleCancelExpense = async (id) => {
    try {
      await api.expenses.cancel(id);
      showToast('Expense reimbursement cancelled');
      loadExpenses();
    } catch (err) {
      showToast(err.message, 'error');
    }
  };

  const filteredEmployees = employees.filter((emp) => {
    const matchesSearch =
      !employeeSearch ||
      `${emp.firstName} ${emp.lastName}`.toLowerCase().includes(employeeSearch.toLowerCase()) ||
      emp.jobTitle.toLowerCase().includes(employeeSearch.toLowerCase()) ||
      emp.email.toLowerCase().includes(employeeSearch.toLowerCase());
    const matchesDept = !employeeDeptFilter || emp.departmentId === employeeDeptFilter;
    return matchesSearch && matchesDept;
  });

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
          <button
            className={`tab-btn ${activeTab === 'employees' ? 'active' : ''}`}
            onClick={() => setActiveTab('employees')}
          >
            <Users size={18} /> Personnel & Teams
          </button>
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
                <h1 className="page-title">Operations Command Center</h1>
                <p className="page-subtitle">Real-time organizational performance metrics, workflows, and workforce telemetry.</p>
              </div>
              <div className="header-actions">
                <button className="btn-primary" onClick={() => setModalOpen('task')}>
                  <Plus size={16} /> New Task
                </button>
                <button className="btn-secondary" onClick={() => setModalOpen('leave')}>
                  <Plus size={16} /> Apply Time Off
                </button>
                <button className="btn-secondary" onClick={() => setModalOpen('expense')}>
                  <Plus size={16} /> Claim Expense
                </button>
              </div>
            </div>

            {/* Executive Summary Stats */}
            {summaryData ? (
              <div className="stats-grid">
                <div className="glass-card stat-card">
                  <div className="stat-icon indigo"><Users size={26} /></div>
                  <div className="stat-content">
                    <span className="stat-label">Active Employees</span>
                    <span className="stat-value">{summaryData.activeEmployees}</span>
                    <span className="stat-sub">Across {summaryData.totalDepartments} Departments</span>
                  </div>
                </div>

                <div className="glass-card stat-card">
                  <div className="stat-icon cyan"><CheckSquare size={26} /></div>
                  <div className="stat-content">
                    <span className="stat-label">Active Tasks</span>
                    <span className="stat-value">
                      {(summaryData.tasksByStatus?.inProgress || 0) + (summaryData.tasksByStatus?.todo || 0) + (summaryData.tasksByStatus?.inReview || 0)}
                    </span>
                    <span className="stat-sub">{summaryData.tasksByStatus?.done || 0} completed</span>
                  </div>
                </div>

                <div className="glass-card stat-card">
                  <div className="stat-icon emerald"><CalendarDays size={26} /></div>
                  <div className="stat-content">
                    <span className="stat-label">Pending Leaves</span>
                    <span className="stat-value">{summaryData.totalPendingLeaves}</span>
                    <span className="stat-sub">{summaryData.totalApprovedLeaves} approved YTD</span>
                  </div>
                </div>

                <div className="glass-card stat-card">
                  <div className="stat-icon amber"><Receipt size={26} /></div>
                  <div className="stat-content">
                    <span className="stat-label">Pending Expenses</span>
                    <span className="stat-value">${Number(summaryData.totalPendingExpensesAmount).toFixed(2)}</span>
                    <span className="stat-sub">${Number(summaryData.totalApprovedExpensesAmount).toFixed(2)} approved</span>
                  </div>
                </div>
              </div>
            ) : (
              <div className="glass-card" style={{ marginBottom: '2rem', padding: '1.25rem', textAlign: 'center' }}>
                <span style={{ color: 'var(--text-muted)' }}>
                  {currentUser ? 'Executive metrics available for Manager/Admin roles' : 'Sign in to view executive dashboard metrics'}
                </span>
              </div>
            )}

            {/* Personal Workspace Widget */}
            {personalData && (
              <div className="glass-card" style={{ marginBottom: '2rem' }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem', flexWrap: 'wrap', gap: '0.5rem' }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
                    <div className="user-avatar" style={{ width: 44, height: 44, fontSize: '1.1rem' }}>
                      {personalData.fullName?.[0] || 'U'}
                    </div>
                    <div>
                      <h3 style={{ fontSize: '1.15rem' }}>{personalData.fullName}</h3>
                      <p style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>
                        {personalData.jobTitle} • {personalData.departmentName || 'Operations'}
                      </p>
                    </div>
                  </div>
                  <div style={{ display: 'flex', gap: '1rem' }}>
                    <div style={{ textAlign: 'right' }}>
                      <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)', textTransform: 'uppercase' }}>Annual Leave Balance</span>
                      <div style={{ fontSize: '1.35rem', fontWeight: 800, color: '#34d399' }}>
                        {personalData.remainingLeaveDays} <span style={{ fontSize: '0.85rem' }}>Days Left</span>
                      </div>
                    </div>
                  </div>
                </div>

                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))', gap: '1rem', borderTop: '1px solid var(--border-subtle)', paddingTop: '1rem' }}>
                  <div>
                    <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>Assigned Tasks</span>
                    <div style={{ fontSize: '1.1rem', fontWeight: 700, marginTop: '0.2rem' }}>
                      {(personalData.assignedTasksByStatus?.inProgress || 0) + (personalData.assignedTasksByStatus?.todo || 0)} In Flight
                    </div>
                  </div>
                  <div>
                    <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>Pending Leave Requests</span>
                    <div style={{ fontSize: '1.1rem', fontWeight: 700, marginTop: '0.2rem' }}>
                      {personalData.pendingLeaveRequestsCount} Pending
                    </div>
                  </div>
                  <div>
                    <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>Expense Claims</span>
                    <div style={{ fontSize: '1.1rem', fontWeight: 700, marginTop: '0.2rem' }}>
                      ${Number(personalData.pendingExpenseReimbursementsTotal || 0).toFixed(2)} Pending
                    </div>
                  </div>
                </div>
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
            <h3 style={{ marginBottom: '1rem', fontSize: '1.1rem' }}>Departments Overview</h3>
            <div className="stats-grid" style={{ marginBottom: '2rem' }}>
              {departments.map((dept) => (
                <div key={dept.id} className="glass-card" style={{ padding: '1.25rem' }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <span className="badge badge-inprogress">{dept.code}</span>
                    <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>{dept.activeEmployeeCount} Staff</span>
                  </div>
                  <h4 style={{ marginTop: '0.65rem', fontSize: '1.1rem' }}>{dept.name}</h4>
                  <p style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginTop: '0.2rem' }}>
                    Manager: {dept.managerName || 'Unassigned'}
                  </p>
                </div>
              ))}
            </div>

            {/* Employee Directory */}
            <h3 style={{ marginBottom: '1rem', fontSize: '1.1rem' }}>Staff Directory</h3>
            <div className="filter-bar">
              <input
                type="text"
                className="search-input"
                placeholder="Search staff by name, title, or email..."
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
                    <th>Job Title</th>
                    <th>Department</th>
                    <th>Hire Date</th>
                    <th>Leave Balance</th>
                    <th>Status</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredEmployees.length === 0 ? (
                    <tr>
                      <td colSpan="6" style={{ textAlign: 'center', padding: '2rem' }}>
                        No staff members matching your filter criteria.
                      </td>
                    </tr>
                  ) : (
                    filteredEmployees.map((emp) => (
                      <tr key={emp.id}>
                        <td>
                          <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
                            <div className="user-avatar" style={{ width: 34, height: 34, fontSize: '0.85rem' }}>
                              {emp.firstName[0]}{emp.lastName[0]}
                            </div>
                            <div>
                              <div style={{ fontWeight: 600 }}>{emp.firstName} {emp.lastName}</div>
                              <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>{emp.email}</div>
                            </div>
                          </div>
                        </td>
                        <td>{emp.jobTitle}</td>
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
                            {lr.startDate.split('T')[0]} → {lr.endDate.split('T')[0]}
                          </td>
                          <td style={{ fontWeight: 700 }}>{lr.daysCount}</td>
                          <td>{lr.reason}</td>
                          <td style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>{lr.createdAtUtc.split('T')[0]}</td>
                          <td>
                            <span className={`badge badge-${lr.status.toLowerCase()}`}>{lr.status}</span>
                          </td>
                          <td style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>{lr.rejectionReason || '—'}</td>
                          <td>
                            {lr.status === 'Pending' && (
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
                          <td style={{ fontFamily: 'var(--font-mono)' }}>{lr.startDate.split('T')[0]} → {lr.endDate.split('T')[0]}</td>
                          <td><span className="badge badge-inprogress">{lr.daysCount} Days</span></td>
                          <td>{lr.reason}</td>
                          <td>
                            <div style={{ display: 'flex', gap: '0.5rem' }}>
                              <button className="btn-success btn-sm" onClick={() => handleApproveLeave(lr.id)}>
                                <CheckCircle2 size={14} /> Approve
                              </button>
                              <button
                                className="btn-danger btn-sm"
                                onClick={() => {
                                  setRejectTargetId(lr.id);
                                  setModalOpen('rejectLeave');
                                }}
                              >
                                <XCircle size={14} /> Reject
                              </button>
                            </div>
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
                const colTasks = tasks.filter((t) => t.status === colStatus);
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
                      colTasks.map((t) => (
                        <div key={t.id} className="task-card">
                          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                            <span className={`badge badge-${t.priority.toLowerCase()}`}>{t.priority}</span>
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
                            {t.status === 'Todo' && (
                              <button className="btn-secondary btn-sm" onClick={() => handleTaskAction(t.id, 'start')}>
                                Start Task
                              </button>
                            )}
                            {t.status === 'InProgress' && (
                              <button className="btn-secondary btn-sm" onClick={() => handleTaskAction(t.id, 'review')}>
                                Submit Review
                              </button>
                            )}
                            {t.status === 'InReview' && (
                              <>
                                <button className="btn-success btn-sm" onClick={() => handleTaskAction(t.id, 'complete')}>
                                  Complete
                                </button>
                                <button className="btn-secondary btn-sm" onClick={() => handleTaskAction(t.id, 'requestChanges')}>
                                  Req. Changes
                                </button>
                              </>
                            )}
                            {t.status !== 'Done' && t.status !== 'Cancelled' && (
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
                      ))
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
                          <td style={{ fontFamily: 'var(--font-mono)' }}>{exp.expenseDate.split('T')[0]}</td>
                          <td><span className="badge badge-inprogress">{exp.category}</span></td>
                          <td style={{ fontWeight: 700 }}>${Number(exp.amount).toFixed(2)} {exp.currency}</td>
                          <td>{exp.description}</td>
                          <td><span className={`badge badge-${exp.status.toLowerCase()}`}>{exp.status}</span></td>
                          <td style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>{exp.rejectionReason || '—'}</td>
                          <td>
                            {exp.status === 'Pending' && (
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
                          <td><span className="badge badge-inprogress">{exp.category}</span></td>
                          <td style={{ fontWeight: 700, color: '#34d399' }}>${Number(exp.amount).toFixed(2)} {exp.currency}</td>
                          <td>{exp.description}</td>
                          <td style={{ fontFamily: 'var(--font-mono)' }}>{exp.expenseDate.split('T')[0]}</td>
                          <td>
                            <div style={{ display: 'flex', gap: '0.5rem' }}>
                              <button className="btn-success btn-sm" onClick={() => handleApproveExpense(exp.id)}>
                                <CheckCircle2 size={14} /> Approve
                              </button>
                              <button
                                className="btn-danger btn-sm"
                                onClick={() => {
                                  setRejectTargetId(exp.id);
                                  setModalOpen('rejectExpense');
                                }}
                              >
                                <XCircle size={14} /> Reject
                              </button>
                            </div>
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
            <p style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginBottom: '0.75rem' }}>
              One-Click Enterprise Demo Profiles:
            </p>
            <div className="demo-accounts">
              <div
                className="demo-account-card"
                onClick={() => handleLogin(null, { username: 'admin', password: 'Admin123!' })}
              >
                <ShieldCheck size={20} color="#818cf8" />
                <span className="demo-role">Admin</span>
                <span className="demo-cred">admin</span>
              </div>
              <div
                className="demo-account-card"
                onClick={() => handleLogin(null, { username: 'manager', password: 'Manager123!' })}
              >
                <UserCheck size={20} color="#22d3ee" />
                <span className="demo-role">Manager</span>
                <span className="demo-cred">manager</span>
              </div>
              <div
                className="demo-account-card"
                onClick={() => handleLogin(null, { username: 'employee', password: 'Employee123!' })}
              >
                <Users size={20} color="#34d399" />
                <span className="demo-role">Employee</span>
                <span className="demo-cred">employee</span>
              </div>
            </div>

            {/* Form */}
            <form onSubmit={authMode === 'login' ? handleLogin : handleRegister}>
              <div className="form-group">
                <label>Username</label>
                <input
                  type="text"
                  required
                  value={authForm.username}
                  onChange={(e) => setAuthForm({ ...authForm, username: e.target.value })}
                  placeholder="e.g. admin"
                />
              </div>

              {authMode === 'register' && (
                <>
                  <div className="form-group">
                    <label>Email Address</label>
                    <input
                      type="email"
                      required
                      value={authForm.email}
                      onChange={(e) => setAuthForm({ ...authForm, email: e.target.value })}
                      placeholder="e.g. user@company.com"
                    />
                  </div>
                  <div className="form-group">
                    <label>Role</label>
                    <select
                      value={authForm.role}
                      onChange={(e) => setAuthForm({ ...authForm, role: e.target.value })}
                    >
                      <option value="Employee">Employee</option>
                      <option value="Manager">Manager</option>
                      <option value="Admin">Admin</option>
                    </select>
                  </div>
                </>
              )}

              <div className="form-group">
                <label>Password</label>
                <input
                  type="password"
                  required
                  value={authForm.password}
                  onChange={(e) => setAuthForm({ ...authForm, password: e.target.value })}
                  placeholder="••••••••"
                />
              </div>

              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: '1.5rem' }}>
                <span
                  style={{ fontSize: '0.85rem', color: 'var(--primary)', cursor: 'pointer' }}
                  onClick={() => setAuthMode(authMode === 'login' ? 'register' : 'login')}
                >
                  {authMode === 'login' ? 'Need an account? Register' : 'Already have an account? Sign In'}
                </span>
                <button type="submit" className="btn-primary">
                  {authMode === 'login' ? 'Sign In' : 'Register'}
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
                <label>Department Code</label>
                <input
                  type="text"
                  required
                  maxLength={10}
                  placeholder="e.g. OPS"
                  value={newDeptForm.code}
                  onChange={(e) => setNewDeptForm({ ...newDeptForm, code: e.target.value })}
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
                <button type="submit" className="btn-primary">Create Department</button>
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
                <label>Job Title</label>
                <input
                  type="text"
                  required
                  placeholder="e.g. Operations Specialist"
                  value={newEmployeeForm.jobTitle}
                  onChange={(e) => setNewEmployeeForm({ ...newEmployeeForm, jobTitle: e.target.value })}
                />
              </div>
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
              <div className="form-row">
                <div className="form-group">
                  <label>Salary (USD)</label>
                  <input
                    type="number"
                    min="0"
                    value={newEmployeeForm.salary}
                    onChange={(e) => setNewEmployeeForm({ ...newEmployeeForm, salary: e.target.value })}
                  />
                </div>
                <div className="form-group">
                  <label>Annual Leave Days</label>
                  <input
                    type="number"
                    min="0"
                    value={newEmployeeForm.annualLeaveBalanceDays}
                    onChange={(e) => setNewEmployeeForm({ ...newEmployeeForm, annualLeaveBalanceDays: e.target.value })}
                  />
                </div>
              </div>
              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem', marginTop: '1.5rem' }}>
                <button type="button" className="btn-secondary" onClick={() => setModalOpen(null)}>Cancel</button>
                <button type="submit" className="btn-primary">Save Profile</button>
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
              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem', marginTop: '1.5rem' }}>
                <button type="button" className="btn-secondary" onClick={() => setModalOpen(null)}>Cancel</button>
                <button type="submit" className="btn-primary">Submit Request</button>
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
                  {employees.map((emp) => (
                    <option key={emp.id} value={emp.id}>{emp.firstName} {emp.lastName} ({emp.jobTitle})</option>
                  ))}
                </select>
              </div>
              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem', marginTop: '1.5rem' }}>
                <button type="button" className="btn-secondary" onClick={() => setModalOpen(null)}>Cancel</button>
                <button type="submit" className="btn-primary">Create Task</button>
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
                    <option value="0">Travel</option>
                    <option value="1">Meals</option>
                    <option value="2">Office Supplies</option>
                    <option value="3">Equipment</option>
                    <option value="4">Software</option>
                    <option value="5">Other</option>
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
                <button type="submit" className="btn-primary">Submit Claim</button>
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
              <button type="button" className="btn-danger" onClick={handleRejectLeaveConfirm}>Confirm Rejection</button>
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
              <button type="button" className="btn-danger" onClick={handleRejectExpenseConfirm}>Confirm Rejection</button>
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
