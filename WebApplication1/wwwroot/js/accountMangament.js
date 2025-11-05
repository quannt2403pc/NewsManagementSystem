document.addEventListener('DOMContentLoaded', () => {
    const accountTableBody = document.getElementById('accountTableBody');
    const addAccountBtn = document.getElementById('addAccountBtn');
    const accountModal = new bootstrap.Modal(document.getElementById('accountModal'));
    const modalTitle = document.getElementById('modalTitle');
    const accountForm = document.getElementById('accountForm');
    const accountIdInput = document.getElementById('accountId');
    const accountNameInput = document.getElementById('accountName');
    const accountEmailInput = document.getElementById('accountEmail');
    const accountRoleInput = document.getElementById('accountRole');
    const accountPasswordInput = document.getElementById('accountPassword');
    const searchInput = document.getElementById('searchInput');
    const roleFilter = document.getElementById('roleFilter');
    const searchBtn = document.getElementById('searchBtn');

    const baseUrl = 'https://localhost:7134/api/Account'; // URL cơ sở cho API Tài khoản
    const jwtToken = localStorage.getItem('jwtToken'); // Lấy token JWT

    if (!jwtToken) {
        window.location.href = 'login.html'; // Chuyển hướng đến trang đăng nhập nếu không có token
        return;
    }

    const headers = {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${jwtToken}`
    };

    // --- Các hàm chính ---
    const fetchAccounts = async (search = '', role = '') => {
        try {
            let url = `${baseUrl}`;
            const queryParams = [];

            // Add search parameter if it's not an empty string
            if (search) {
                queryParams.push(`search=${search}`);
            }

            // Add role parameter only if it has a valid value
            // The empty string from the select box is intentionaly ignored here
            if (role) {
                if (role) {
                    queryParams.push(`role=${parseInt(role)}`);
                }
            }

            // Build the final URL
            if (queryParams.length > 0) {
                url += `?${queryParams.join('&')}`;
            }

            const response = await fetch(url, { headers });
            console.log(response)
            if (!response.ok) {
                const errorData = await response.json();
                console.error('API Error:', errorData);
                return;
            }

            const data = await response.json();
            const accounts = Array.isArray(data) ? data : data.$values || [];

            renderTable(accounts);
        } catch (error) {
            console.error('Error fetching accounts:', error);
            alert('An error occurred while fetching data.');
        }
    };

    const renderTable = (accounts) => {
        accountTableBody.innerHTML = '';
        if (accounts.length === 0) {
            accountTableBody.innerHTML = '<tr><td colspan="5" class="text-center">Không tìm thấy tài khoản nào.</td></tr>';
            return;
        }

        accounts.forEach(account => {
            const row = `
                <tr>
                    <td>${account.accountId}</td>
                    <td>${account.accountName}</td>
                    <td>${account.accountEmail}</td>
                    <td>${account.accountRole === 1 ? 'Staff' : 'Lecturer'}</td>
                    <td>
                        <button class="btn btn-warning btn-sm edit-btn" data-id="${account.accountId}">Sửa</button>
                        <button class="btn btn-danger btn-sm delete-btn" data-id="${account.accountId}">Xóa</button>
                    </td>
                </tr>
            `;
            accountTableBody.innerHTML += row;
        });

        document.querySelectorAll('.edit-btn').forEach(btn => btn.addEventListener('click', handleEdit));
        document.querySelectorAll('.delete-btn').forEach(btn => btn.addEventListener('click', handleDelete));
    };

    const handleEdit = async (e) => {
        const id = e.target.dataset.id;
        try {
            const response = await fetch(`${baseUrl}/${id}`, { headers });
            if (!response.ok) throw new Error('Không thể lấy tài khoản để sửa.');
            const account = await response.json();

            modalTitle.textContent = 'Sửa tài khoản';
            accountIdInput.value = account.accountId;
            accountNameInput.value = account.accountName;
            accountEmailInput.value = account.accountEmail;
            accountRoleInput.value = account.accountRole;
            accountPasswordInput.removeAttribute('required'); // Mật khẩu không bắt buộc khi sửa
            accountModal.show();
        } catch (error) {
            console.error('Lỗi khi sửa tài khoản:', error);
            alert('Không thể tải dữ liệu tài khoản để sửa.');
        }
    };

    const handleDelete = async (e) => {
        const id = e.target.dataset.id;
        if (confirm('Bạn có chắc chắn muốn xóa tài khoản này không?')) {
            try {
                const response = await fetch(`${baseUrl}/${id}`, { method: 'DELETE', headers });
                if (!response.ok) {
                    const error = await response.json();
                    alert(error.message || 'Xóa tài khoản thất bại.');
                    return;
                }
                alert('Xóa tài khoản thành công!');
                fetchAccounts(); // Tải lại bảng
            } catch (error) {
                console.error('Lỗi khi xóa tài khoản:', error);
                alert('Đã xảy ra lỗi trong quá trình xóa.');
            }
        }
    };

    const handleFormSubmit = async (e) => {
        e.preventDefault();
        const id = accountIdInput.value;
        const account = {
            accountName: accountNameInput.value,
            accountEmail: accountEmailInput.value,
            accountRole: parseInt(accountRoleInput.value),
            accountPassword: accountPasswordInput.value
        };

        try {
            if (id) { // Sửa
                account.accountId = parseInt(id);
                // Chỉ gửi mật khẩu nếu nó không rỗng
                if (account.accountPassword) {
                    // Trong ứng dụng thực tế, gửi yêu cầu cập nhật mật khẩu riêng
                } else {
                    delete account.accountPassword;
                }
                const response = await fetch(`${baseUrl}/${id}`, {
                    method: 'PUT',
                    headers,
                    body: JSON.stringify(account)
                });
                if (!response.ok) throw new Error('Cập nhật tài khoản thất bại.');
            } else { // Thêm
                const response = await fetch(baseUrl, {
                    method: 'POST',
                    headers,
                    body: JSON.stringify(account)
                });
                if (!response.ok) {
                    const error = await response.json();
                    alert(error.message || 'Thêm tài khoản thất bại.');
                    return;
                }
            }
            accountModal.hide();
            fetchAccounts(); // Tải lại bảng
        } catch (error) {
            console.error('Lỗi khi lưu tài khoản:', error);
            alert('Đã xảy ra lỗi khi lưu tài khoản.');
        }
    };
    document.getElementById('logoutBtn').addEventListener('click', () => {
        localStorage.removeItem("jwtToken");
        window.location.href = "/ui/login.html";
    });
    // --- Trình lắng nghe sự kiện ---

    addAccountBtn.addEventListener('click', () => {
        accountForm.reset();
        modalTitle.textContent = 'Thêm tài khoản mới';
        accountIdInput.value = '';
        accountPasswordInput.setAttribute('required', 'required');
        accountModal.show();
    });

    searchBtn.addEventListener('click', () => {
        const search = searchInput.value;
        const role = roleFilter.value;
        fetchAccounts(search, role);
    });

    accountForm.addEventListener('submit', handleFormSubmit);

    // Lần tải ban đầu
    fetchAccounts();
});