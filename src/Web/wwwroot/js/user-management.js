// User Management JavaScript Module

// Get anti-forgery token
function getToken() {
    // Prefer a global token if present
    const pageToken = document.querySelector('input[name="__RequestVerificationToken"]');
    if (pageToken && pageToken.value) return pageToken.value;
    // Fallback: search within visible modals/forms
    const modalToken = document.querySelector('.modal.show input[name="__RequestVerificationToken"]')
        || document.querySelector('form input[name="__RequestVerificationToken"]');
    return modalToken ? modalToken.value : '';
}

// Create new user
async function createUser() {
    const form = document.getElementById('createUserForm');
    const formData = new FormData(form);
    
    const userData = {
        firstName: formData.get('firstName'),
        lastName: formData.get('lastName'),
        email: formData.get('email'),
        organization: formData.get('organization'),
        idNumber: formData.get('idNumber'),
        cell: formData.get('cell'),
        address: formData.get('address'),
        role: formData.get('role')
    };

    try {
        const response = await fetch('/Approvals/CreateUser', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getToken()
            },
            body: JSON.stringify(userData)
        });

        const result = await response.json();
        if (result.success) {
            $('#createUserModal').modal('hide');
            
            if (result.user && result.user.tempPassword) {
                $('#resetUser').text(result.user.email);
                $('#tempPassword').text(result.user.tempPassword);
                $('#passwordModal').modal('show');
            }
            
            setTimeout(() => location.reload(), 2000);
        } else {
            alert('Error: ' + result.message);
        }
    } catch (error) {
        console.error('Error creating user:', error);
        alert('An error occurred while creating the user');
    }
}

// Open edit modal
async function openEditModal(userId) {
    try {
        const response = await fetch(`/Approvals/GetUserDetails?id=${userId}`);
        const result = await response.json();
        
        if (result.success && result.user) {
            const form = document.getElementById('editUserForm');
            form.elements['id'].value = result.user.id;
            form.elements['firstName'].value = result.user.firstName;
            form.elements['lastName'].value = result.user.lastName;
            form.elements['email'].value = result.user.email;
            form.elements['organization'].value = result.user.organization;
            form.elements['idNumber'].value = result.user.idNumber;
            form.elements['cell'].value = result.user.cell;
            form.elements['address'].value = result.user.address;
            
            if (result.user.roles && result.user.roles.length > 0) {
                form.elements['role'].value = result.user.roles[0];
            }
            
            $('#editUserModal').modal('show');
        } else {
            alert('Error loading user details');
        }
    } catch (error) {
        console.error('Error:', error);
        alert('An error occurred');
    }
}

// Update user
async function updateUser() {
    const form = document.getElementById('editUserForm');
    const formData = new FormData(form);
    
    const userData = {
        id: formData.get('id'),
        firstName: formData.get('firstName'),
        lastName: formData.get('lastName'),
        email: formData.get('email'),
        organization: formData.get('organization'),
        idNumber: formData.get('idNumber'),
        cell: formData.get('cell'),
        address: formData.get('address'),
        role: formData.get('role') || null
    };

    try {
        const response = await fetch('/Approvals/UpdateUser', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getToken()
            },
            body: JSON.stringify(userData)
        });

        const result = await response.json();
        if (result.success) {
            $('#editUserModal').modal('hide');
            location.reload();
        } else {
            alert('Error: ' + result.message);
        }
    } catch (error) {
        console.error('Error:', error);
        alert('An error occurred');
    }
}

// Reset password
async function resetPassword(userId) {
    if (!confirm('Reset password for this user?')) return;

    try {
        const response = await fetch(`/Approvals/ResetPassword?id=${userId}`, {
            method: 'POST',
            headers: {
                'RequestVerificationToken': getToken()
            }
        });

        const result = await response.json();
        if (result.success) {
            $('#resetUser').text(result.userName);
            $('#tempPassword').text(result.tempPassword);
            $('#passwordModal').modal('show');
        } else {
            alert('Error: ' + result.message);
        }
    } catch (error) {
        console.error('Error:', error);
        alert('An error occurred');
    }
}

// Toggle lockout
async function toggleLockout(userId, shouldLock) {
    const action = shouldLock ? 'lock' : 'unlock';
    if (!confirm(`Are you sure you want to ${action} this user?`)) return;

    try {
        const response = await fetch(`/Approvals/ToggleLockout?id=${userId}&locked=${shouldLock}`, {
            method: 'POST',
            headers: {
                'RequestVerificationToken': getToken()
            }
        });

        const result = await response.json();
        if (result.success) {
            location.reload();
        } else {
            alert('Error: ' + result.message);
        }
    } catch (error) {
        console.error('Error:', error);
        alert('An error occurred');
    }
}

// Copy password
function copyPassword() {
    const password = document.getElementById('tempPassword').textContent;
    navigator.clipboard.writeText(password).then(() => {
        const btn = event.target;
        const originalText = btn.textContent;
        btn.textContent = 'Copied!';
        setTimeout(() => {
            btn.textContent = originalText;
        }, 2000);
    });
}

// Edit before approve
function editBeforeApprove(id, email, firstName, lastName, organization, idNumber, cell) {
    // Pre-fill the edit form with approval request data
    const form = document.getElementById('editUserForm');
    form.elements['id'].value = id;
    form.elements['firstName'].value = firstName;
    form.elements['lastName'].value = lastName;
    form.elements['email'].value = email;
    form.elements['organization'].value = organization;
    form.elements['idNumber'].value = idNumber;
    form.elements['cell'].value = cell;
    
    $('#editUserModal').modal('show');
    
    // Override save button to approve after edit
    const saveBtn = document.querySelector('#editUserModal .btn-primary');
    saveBtn.onclick = async function() {
        await updateAndApprove(id);
    };
}

// Update and approve
async function updateAndApprove(approvalId) {
    const form = document.getElementById('editUserForm');
    const formData = new FormData(form);
    
    const data = {
        approvalId: approvalId,
        userEdit: {
            firstName: formData.get('firstName'),
            lastName: formData.get('lastName'),
            email: formData.get('email'),
            organization: formData.get('organization'),
            idNumber: formData.get('idNumber'),
            cell: formData.get('cell'),
            address: formData.get('address')
        }
    };

    try {
        const response = await fetch('/Approvals/ApproveWithEdit', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getToken()
            },
            body: JSON.stringify(data)
        });

        const result = await response.json();
        if (result.success) {
            $('#editUserModal').modal('hide');
            location.reload();
        } else {
            alert('Error: ' + result.message);
        }
    } catch (error) {
        console.error('Error:', error);
        alert('An error occurred');
    }
}

// Initialize DataTable when document is ready
$(document).ready(function() {
    if ($.fn.DataTable && $('#users table').length) {
        $('#users table').DataTable({
            "pageLength": 25,
            "order": [[0, "asc"]],
            "columnDefs": [
                { "orderable": false, "targets": 5 }
            ]
        });
    }
});
