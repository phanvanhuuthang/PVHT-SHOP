# Tạo Solution
Tạo solution có tên SV.<Mã Sinh Viên>
BỔ sung cho solution này các project sau:
- <Tên solution>.Admin: project có dạng ASP.NET Core MVC
- <Tên solution>.Web: project có dạng ASP.NET Core MVC
- <Tên solution>.Shop: project có dạng Class Library
- <Tên solution>.DomainModels: project có dạng Class Library
- <Tên solution>.BusinessLayers: project có dạng Class Library
# Thiết kế Layout cho SV.Admin
- Sử dụng theme AdminLTE4
- THiết kế file _Layout.cshtml
	+ Thiết kế file _Header.cshtml,_SideBar.cshtml,_Footer.cshtml
	+ Sử dụng @RenderBody() để hiển thị nội dung chính có thể thay đổi
- Liên kết các chức năng dự kiến trên Layout(Header,SideBar)
# Các controller, Action dự kiến cho chứ năng
## Account : Các chức năng liên quan đến tài khoản (Cá nhân)
- Account/Login
- Account/Logout
- Account/ChangePassword
## Supplier: Các chức năng liên quan đến quản lý nhà cung cấp
- Supplier/Index: trang
	- Hiển thị danh sách nhà cung cấp dưới dạng bảng, có phân trang
	- Tìm kiếm nhà cung cấp theo tên
	- Điều hướng đến các chức năng bổ sung, cập nhật, xóa nhà cung cấp
- Supplier/Create:
- Supplier/Edit/{id}
- Supplier/Delete/{id}

## Customer: Các chức năng liên quan đến quản lý khách hàng
- Customer/Index
- Customer/Create
- Customer/Edit/{id}
- Customer/Delete/{id}
- Customer/ChangePassword/{id}

## Shipper: Các chức năng liên quan đến quản lý người giao hàng
- Shipper/Index
- Shipper/Create
- Shipper/Edit/{id}
- Shipper/Delete/{id}
## Employee: Các chức năng liên quan đến Nhân viên
- Employee/Index
- Employee/Create
- Employee/Edit/{id}
- Employee/Delete/{id}
- Employee/ChangePassword/{id}
- Employee/ChangeRole/{id}
## Category: Các chức năng liên quan đến Loại hàng
- Category/Index
- Category/Create
- Category/Edit/{id}
- Category/Delete/{id}
## Product: Các chức năng liên quan đến Mặt hàng
- Product/Index
- Product/Detail/{id}
- Product/Create
- Product/Edit/{id}
- Product/Delete/{id}
- Product/ListAttribute/{id}
- Product/AddAttribute/{id}
- Product/EditAttribute/{id}?attributeId={attributeId}
- Product/DeleteAttribute/{id}?attributeId={attributeId}
- Product/ListPhotos/{id}
- Product/AddPhoto/{id}
- Product/EditPhoto/{id}?photoId={photoId}
- Product/DeletePhoto/{id}?photoId={photoId}
## Order: Các chức năng liên quan đến Đơn hàng
Cơ chế chung (đăng nhập,kiểm tra quyền)
- Người sử dụng cung cấp thông tin để kiểm tra xem có được phép truy cập không?(username+password)/vân tay/khuôn mặt)
- Hệ thống kiểm tra xem có hợp lệ không?