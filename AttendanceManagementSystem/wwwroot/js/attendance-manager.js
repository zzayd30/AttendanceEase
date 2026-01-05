// Attendance Management JavaScript with AJAX functionality
class AttendanceManager {
    constructor() {
        this.apiBaseUrl = '/api/attendance';
        this.init();
    }

    init() {
        this.setupEventListeners();
        this.loadInitialData();
    }

    setupEventListeners() {
        // Course selection change
        const courseSelect = document.getElementById('courseSelect');
        if (courseSelect) {
            courseSelect.addEventListener('change', (e) => {
                this.loadStudentsByCourse(e.target.value);
            });
        }

        // Mark all present/absent buttons
        const markAllPresentBtn = document.getElementById('markAllPresent');
        const markAllAbsentBtn = document.getElementById('markAllAbsent');
        
        if (markAllPresentBtn) {
            markAllPresentBtn.addEventListener('click', () => this.markAllStudents('Present'));
        }
        
        if (markAllAbsentBtn) {
            markAllAbsentBtn.addEventListener('click', () => this.markAllStudents('Absent'));
        }

        // Submit attendance form
        const attendanceForm = document.getElementById('attendanceForm');
        if (attendanceForm) {
            attendanceForm.addEventListener('submit', (e) => this.handleAttendanceSubmit(e));
        }

        // Date picker change
        const datePicker = document.getElementById('attendanceDate');
        if (datePicker) {
            datePicker.addEventListener('change', (e) => {
                this.loadAttendanceForDate(e.target.value);
            });
        }
    }

    async loadInitialData() {
        try {
            // Load students for the selected course if any
            const courseSelect = document.getElementById('courseSelect');
            if (courseSelect && courseSelect.value) {
                await this.loadStudentsByCourse(courseSelect.value);
            }
        } catch (error) {
            console.error('Error loading initial data:', error);
        }
    }

    async loadStudentsByCourse(courseId) {
        if (!courseId) {
            this.clearStudentsTable();
            return;
        }

        try {
            this.showLoading('Loading students...');
            
            const response = await fetch(`${this.apiBaseUrl}/students/${courseId}`, {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json'
                }
            });

            const result = await response.json();
            
            if (result.success) {
                this.displayStudents(result.data);
                this.showMessage('Students loaded successfully', 'success');
            } else {
                this.showMessage(result.message || 'Failed to load students', 'error');
            }
        } catch (error) {
            console.error('Error loading students:', error);
            this.showMessage('An error occurred while loading students', 'error');
        } finally {
            this.hideLoading();
        }
    }

    displayStudents(students) {
        const tableBody = document.getElementById('studentsTableBody');
        if (!tableBody) return;

        if (!students || students.length === 0) {
            tableBody.innerHTML = `
                <tr>
                    <td colspan="6" class="text-center text-muted">
                        <i class="bi bi-person-x"></i> No students found for this course
                    </td>
                </tr>
            `;
            return;
        }

        tableBody.innerHTML = students.map((student, index) => `
            <tr>
                <td>${index + 1}</td>
                <td>
                    <div class="d-flex align-items-center">
                        <div class="avatar-sm bg-primary rounded-circle d-flex align-items-center justify-content-center text-white me-2">
                            ${student.name.charAt(0).toUpperCase()}
                        </div>
                        <div>
                            <div class="fw-semibold">${this.escapeHtml(student.name)}</div>
                            <small class="text-muted">${this.escapeHtml(student.rollNumber)}</small>
                        </div>
                    </div>
                </td>
                <td>
                    <small class="text-muted">${this.escapeHtml(student.email)}</small>
                </td>
                <td>
                    <div class="btn-group" role="group">
                        <input type="radio" class="btn-check" name="attendance_${student.id}" id="present_${student.id}" value="Present">
                        <label class="btn btn-outline-success btn-sm" for="present_${student.id}">
                            <i class="bi bi-check-circle"></i> Present
                        </label>
                        
                        <input type="radio" class="btn-check" name="attendance_${student.id}" id="absent_${student.id}" value="Absent">
                        <label class="btn btn-outline-danger btn-sm" for="absent_${student.id}">
                            <i class="bi bi-x-circle"></i> Absent
                        </label>
                        
                        <input type="radio" class="btn-check" name="attendance_${student.id}" id="late_${student.id}" value="Late">
                        <label class="btn btn-outline-warning btn-sm" for="late_${student.id}">
                            <i class="bi bi-clock"></i> Late
                        </label>
                    </div>
                </td>
                <td>
                    <input type="text" class="form-control form-control-sm" id="remarks_${student.id}" placeholder="Optional remarks">
                </td>
                <td>
                    ${student.lastAttendance ? `
                        <span class="badge bg-${this.getStatusBadgeClass(student.lastAttendance.status)}">
                            ${student.lastAttendance.status}
                        </span>
                        <br><small class="text-muted">${new Date(student.lastAttendance.date).toLocaleDateString()}</small>
                    ` : '<small class="text-muted">No previous record</small>'}
                </td>
            </tr>
        `).join('');

        // Show action buttons
        this.showActionButtons();
    }

    getStatusBadgeClass(status) {
        switch (status) {
            case 'Present': return 'success';
            case 'Absent': return 'danger';
            case 'Late': return 'warning';
            case 'Excused': return 'info';
            default: return 'secondary';
        }
    }

    markAllStudents(status) {
        const checkboxes = document.querySelectorAll('input[type="radio"][value="' + status + '"]');
        checkboxes.forEach(checkbox => {
            checkbox.checked = true;
        });
        
        this.showMessage(`All students marked as ${status}`, 'success');
    }

    async handleAttendanceSubmit(event) {
        event.preventDefault();
        
        try {
            const courseSelect = document.getElementById('courseSelect');
            if (!courseSelect || !courseSelect.value) {
                this.showMessage('Please select a course', 'error');
                return;
            }

            const attendanceData = this.collectAttendanceData();
            if (attendanceData.length === 0) {
                this.showMessage('Please mark attendance for at least one student', 'error');
                return;
            }

            this.showLoading('Saving attendance...');

            const response = await fetch(`${this.apiBaseUrl}/mark`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    courseId: parseInt(courseSelect.value),
                    studentAttendances: attendanceData
                })
            });

            const result = await response.json();
            
            if (result.success) {
                this.showMessage(result.message || 'Attendance saved successfully', 'success');
                this.resetForm();
            } else {
                this.showMessage(result.message || 'Failed to save attendance', 'error');
            }
        } catch (error) {
            console.error('Error saving attendance:', error);
            this.showMessage('An error occurred while saving attendance', 'error');
        } finally {
            this.hideLoading();
        }
    }

    collectAttendanceData() {
        const attendanceData = [];
        const tableBody = document.getElementById('studentsTableBody');
        
        if (!tableBody) return attendanceData;

        const rows = tableBody.querySelectorAll('tr');
        rows.forEach(row => {
            const checkedRadio = row.querySelector('input[type="radio"]:checked');
            if (checkedRadio) {
                const studentId = this.extractStudentId(checkedRadio.name);
                const status = checkedRadio.value;
                const remarksInput = row.querySelector(`#remarks_${studentId}`);
                const remarks = remarksInput ? remarksInput.value.trim() : '';

                attendanceData.push({
                    studentId: parseInt(studentId),
                    status: status,
                    remarks: remarks || null
                });
            }
        });

        return attendanceData;
    }

    extractStudentId(radioName) {
        // Extract student ID from radio name (attendance_123 -> 123)
        return radioName.split('_')[1];
    }

    async loadAttendanceForDate(date) {
        const courseSelect = document.getElementById('courseSelect');
        if (!courseSelect || !courseSelect.value || !date) return;

        try {
            this.showLoading('Loading attendance for selected date...');
            
            const response = await fetch(`${this.apiBaseUrl}/course-summary/${courseSelect.value}?date=${date}`, {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json'
                }
            });

            const result = await response.json();
            
            if (result.success) {
                this.displayExistingAttendance(result.data.students);
                this.displayAttendanceSummary(result.data.summary);
            }
        } catch (error) {
            console.error('Error loading attendance for date:', error);
        } finally {
            this.hideLoading();
        }
    }

    displayExistingAttendance(students) {
        students.forEach(student => {
            if (student.attendance && student.attendance.status) {
                const radioButton = document.getElementById(`${student.attendance.status.toLowerCase()}_${student.id}`);
                if (radioButton) {
                    radioButton.checked = true;
                }

                const remarksInput = document.getElementById(`remarks_${student.id}`);
                if (remarksInput && student.attendance.remarks) {
                    remarksInput.value = student.attendance.remarks;
                }
            }
        });
    }

    displayAttendanceSummary(summary) {
        const summaryContainer = document.getElementById('attendanceSummary');
        if (!summaryContainer) return;

        summaryContainer.innerHTML = `
            <div class="row g-3">
                <div class="col-md-3">
                    <div class="card border-0 bg-primary bg-gradient text-white">
                        <div class="card-body text-center">
                            <h3 class="mb-0">${summary.totalStudents}</h3>
                            <small>Total Students</small>
                        </div>
                    </div>
                </div>
                <div class="col-md-3">
                    <div class="card border-0 bg-success bg-gradient text-white">
                        <div class="card-body text-center">
                            <h3 class="mb-0">${summary.presentStudents}</h3>
                            <small>Present</small>
                        </div>
                    </div>
                </div>
                <div class="col-md-3">
                    <div class="card border-0 bg-danger bg-gradient text-white">
                        <div class="card-body text-center">
                            <h3 class="mb-0">${summary.absentStudents}</h3>
                            <small>Absent</small>
                        </div>
                    </div>
                </div>
                <div class="col-md-3">
                    <div class="card border-0 bg-info bg-gradient text-white">
                        <div class="card-body text-center">
                            <h3 class="mb-0">${summary.attendancePercentage}%</h3>
                            <small>Attendance Rate</small>
                        </div>
                    </div>
                </div>
            </div>
        `;
    }

    clearStudentsTable() {
        const tableBody = document.getElementById('studentsTableBody');
        if (tableBody) {
            tableBody.innerHTML = `
                <tr>
                    <td colspan="6" class="text-center text-muted">
                        <i class="bi bi-arrow-up"></i> Select a course to view students
                    </td>
                </tr>
            `;
        }
        this.hideActionButtons();
    }

    showActionButtons() {
        const actionButtons = document.getElementById('attendanceActions');
        if (actionButtons) {
            actionButtons.style.display = 'block';
        }
    }

    hideActionButtons() {
        const actionButtons = document.getElementById('attendanceActions');
        if (actionButtons) {
            actionButtons.style.display = 'none';
        }
    }

    resetForm() {
        const form = document.getElementById('attendanceForm');
        if (form) {
            form.reset();
        }
        
        // Clear all radio buttons
        const radioButtons = document.querySelectorAll('input[type="radio"]');
        radioButtons.forEach(radio => radio.checked = false);
        
        // Clear all remarks
        const remarksInputs = document.querySelectorAll('input[id^="remarks_"]');
        remarksInputs.forEach(input => input.value = '');
    }

    showLoading(message = 'Loading...') {
        this.hideMessage();
        
        let loadingElement = document.getElementById('loadingIndicator');
        if (!loadingElement) {
            loadingElement = document.createElement('div');
            loadingElement.id = 'loadingIndicator';
            loadingElement.className = 'alert alert-info d-flex align-items-center';
            loadingElement.innerHTML = `
                <div class="spinner-border spinner-border-sm me-2" role="status">
                    <span class="visually-hidden">Loading...</span>
                </div>
                <span id="loadingMessage">${message}</span>
            `;
            
            const container = document.querySelector('.attendance-container') || document.body;
            container.insertBefore(loadingElement, container.firstChild);
        } else {
            document.getElementById('loadingMessage').textContent = message;
            loadingElement.style.display = 'flex';
        }
    }

    hideLoading() {
        const loadingElement = document.getElementById('loadingIndicator');
        if (loadingElement) {
            loadingElement.style.display = 'none';
        }
    }

    showMessage(message, type = 'info') {
        this.hideMessage();
        
        const alertClass = type === 'error' ? 'alert-danger' : 
                          type === 'success' ? 'alert-success' : 
                          type === 'warning' ? 'alert-warning' : 'alert-info';
        
        const icon = type === 'error' ? 'exclamation-triangle' : 
                    type === 'success' ? 'check-circle' : 
                    type === 'warning' ? 'exclamation-triangle' : 'info-circle';
        
        const messageElement = document.createElement('div');
        messageElement.id = 'messageAlert';
        messageElement.className = `alert ${alertClass} alert-dismissible fade show`;
        messageElement.innerHTML = `
            <i class="bi bi-${icon} me-2"></i>
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        `;
        
        const container = document.querySelector('.attendance-container') || document.body;
        container.insertBefore(messageElement, container.firstChild);
        
        // Auto-remove after 5 seconds for success messages
        if (type === 'success') {
            setTimeout(() => this.hideMessage(), 5000);
        }
    }

    hideMessage() {
        const messageElement = document.getElementById('messageAlert');
        if (messageElement) {
            messageElement.remove();
        }
    }

    escapeHtml(text) {
        const map = {
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#039;'
        };
        return text.replace(/[&<>"']/g, (m) => map[m]);
    }
}

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    // Initialize attendance manager if we're on an attendance page
    if (document.getElementById('attendanceForm') || document.getElementById('courseSelect')) {
        new AttendanceManager();
    }
});

// Export for use in other scripts
window.AttendanceManager = AttendanceManager;