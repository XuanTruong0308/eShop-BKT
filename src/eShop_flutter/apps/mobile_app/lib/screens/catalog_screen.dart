import 'dart:ui';
import 'package:flutter/material.dart';
import 'package:flutter_svg/flutter_svg.dart';
import 'package:api_catalog/api_catalog.dart';
import 'package:api_basket/api_basket.dart';
import 'package:core_network/core_network.dart';
import 'package:intl/intl.dart';
import 'login_screen.dart';
import 'basket_screen.dart';
import '../session_state.dart';
import '../theme.dart';
import '../config.dart';

class CatalogScreen extends StatefulWidget {
  const CatalogScreen({super.key});

  @override
  State<CatalogScreen> createState() => _CatalogScreenState();
}

class _CatalogScreenState extends State<CatalogScreen> {
  // Kết nối tới Catalog API chạy ở cổng 5222 của Backend C#
  final _catalogApi = CatalogApi(
    NetworkClient(baseUrl: AppConfig.apiBaseUrl, getToken: () async => SessionState.token),
  );

  final _basketApi = BasketApi(
    NetworkClient(baseUrl: AppConfig.apiBaseUrl, getToken: () async => SessionState.token),
  );

  final ScrollController _scrollController = ScrollController();
  final _currencyFormatter = NumberFormat.simpleCurrency(name: 'USD');

  bool _isLoading = true;
  bool _isFiltering = false;
  bool _isLoadMoreRunning = false;
  bool _hasMoreItems = true;
  String? _error;
  String? _filterError;
  bool _showBackToTop = false; // Trạng thái hiển thị nút cuộn lên đầu

  List<CatalogItem> _items = [];
  List<CatalogBrand> _brands = [];
  List<CatalogType> _types = [];

  int? _selectedBrandId;
  int? _selectedTypeId;

  int _currentPageIndex = 0;
  final int _pageSize = 10; // Đặt kích thước trang là 10 sản phẩm
  int _cartCount = 0; // State đếm số sản phẩm thực tế trong giỏ hàng

  @override
  void initState() {
    super.initState();
    _scrollController.addListener(_scrollListener);
    _loadInitialData();
  }

  @override
  void dispose() {
    _scrollController.dispose();
    super.dispose();
  }

  void _scrollListener() {
    // Tự động tải thêm sản phẩm khi cuộn gần cuối
    if (_scrollController.position.pixels >=
        _scrollController.position.maxScrollExtent - 200) {
      _loadMoreItems();
    }

    // Hiển thị nút "Cuộn lên đầu" khi cuộn qua 500px
    if (_scrollController.offset >= 500) {
      if (!_showBackToTop) {
        setState(() => _showBackToTop = true);
      }
    } else {
      if (_showBackToTop) {
        setState(() => _showBackToTop = false);
      }
    }
  }

  Future<void> _loadInitialData() async {
    setState(() {
      _isLoading = true;
      _error = null;
      _currentPageIndex = 0;
      _hasMoreItems = true;
    });

    try {
      final results = await Future.wait([
        _catalogApi.getBrands(),
        _catalogApi.getTypes(),
        _catalogApi.getCatalogItems(
          pageIndex: 0,
          pageSize: _pageSize,
          brandId: _selectedBrandId,
          typeId: _selectedTypeId,
        ),
      ]);

      final itemsResult = results[2] as CatalogResult;

      setState(() {
        _brands = results[0] as List<CatalogBrand>;
        _types = results[1] as List<CatalogType>;
        _items = itemsResult.data;
        _hasMoreItems = itemsResult.data.length >= _pageSize;
        _isLoading = false;
      });
      _loadBasket();
    } catch (e) {
      setState(() {
        _error =
            'Không thể kết nối đến hệ thống Catalog. Vui lòng kiểm tra API (cổng 5222)!';
        _isLoading = false;
      });
    }
  }

  Future<void> _loadBasket() async {
    try {
      final basket = await _basketApi.getBasket();
      final totalQty = basket.items.fold<int>(
        0,
        (sum, item) => sum + item.quantity,
      );
      setState(() {
        _cartCount = totalQty;
      });
    } catch (e) {
      // Bỏ qua lỗi hoặc log trong môi trường offline
    }
  }

  Future<void> _loadFilteredItems() async {
    setState(() {
      _isFiltering = true;
      _filterError = null;
      _currentPageIndex = 0;
      _hasMoreItems = true;
    });

    try {
      final result = await _catalogApi.getCatalogItems(
        pageIndex: 0,
        pageSize: _pageSize,
        brandId: _selectedBrandId,
        typeId: _selectedTypeId,
      );
      setState(() {
        _items = result.data;
        _hasMoreItems = result.data.length >= _pageSize;
        _isFiltering = false;
      });
    } catch (e) {
      setState(() {
        _filterError = 'Lỗi khi lọc danh sách sản phẩm. Vui lòng thử lại!';
        _isFiltering = false;
      });
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Lỗi khi lọc: $e'),
            backgroundColor: Colors.redAccent,
            behavior: SnackBarBehavior.floating,
          ),
        );
      }
    }
  }

  Future<void> _loadMoreItems() async {
    if (_isLoading || _isLoadMoreRunning || !_hasMoreItems) return;

    setState(() {
      _isLoadMoreRunning = true;
    });

    try {
      final nextPage = _currentPageIndex + 1;
      final result = await _catalogApi.getCatalogItems(
        pageIndex: nextPage,
        pageSize: _pageSize,
        brandId: _selectedBrandId,
        typeId: _selectedTypeId,
      );

      setState(() {
        _currentPageIndex = nextPage;
        _items.addAll(result.data);
        _hasMoreItems = result.data.length >= _pageSize;
        _isLoadMoreRunning = false;
      });
    } catch (e) {
      setState(() {
        _isLoadMoreRunning = false;
      });
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Lỗi tải thêm sản phẩm. Vui lòng kiểm tra kết nối!'),
            behavior: SnackBarBehavior.floating,
          ),
        );
      }
    }
  }

  void _onBrandSelected(int? brandId) {
    if (_selectedBrandId == brandId) return;
    setState(() => _selectedBrandId = brandId);
    _loadFilteredItems();
  }

  void _onTypeSelected(int? typeId) {
    if (_selectedTypeId == typeId) return;
    setState(() => _selectedTypeId = typeId);
    _loadFilteredItems();
  }

  Future<void> _addToCart(CatalogItem item) async {
    try {
      CustomerBasket basket;
      try {
        basket = await _basketApi.getBasket();
      } catch (e) {
        basket = CustomerBasket(buyerId: 'alice', items: []);
      }

      final existingIndex = basket.items.indexWhere(
        (i) => i.productId == item.id,
      );
      final List<BasketItem> updatedItems = List.from(basket.items);

      if (existingIndex >= 0) {
        final existingItem = basket.items[existingIndex];
        updatedItems[existingIndex] = BasketItem(
          id: existingItem.id,
          productId: existingItem.productId,
          productName: existingItem.productName,
          unitPrice: existingItem.unitPrice,
          oldUnitPrice: existingItem.oldUnitPrice,
          quantity: existingItem.quantity + 1,
          pictureUrl: existingItem.pictureUrl,
        );
      } else {
        updatedItems.add(
          BasketItem(
            productId: item.id,
            productName: item.name,
            unitPrice: item.price,
            quantity: 1,
            pictureUrl:
                '${AppConfig.apiBaseUrl}/api/catalog/items/${item.id}/pic?api-version=2.0',
          ),
        );
      }

      final updatedBasket = await _basketApi.updateBasket(
        CustomerBasket(buyerId: basket.buyerId, items: updatedItems),
      );

      final totalQty = updatedBasket.items.fold<int>(
        0,
        (sum, i) => sum + i.quantity,
      );
      setState(() {
        _cartCount = totalQty;
      });

      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Đã thêm ${item.name} vào giỏ hàng!'),
            duration: const Duration(seconds: 1),
            behavior: SnackBarBehavior.floating,
          ),
        );
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Không thể thêm vào giỏ hàng: $e'),
            backgroundColor: Colors.redAccent,
            behavior: SnackBarBehavior.floating,
          ),
        );
      }
    }
  }

  void _logout() {
    Navigator.pushReplacement(
      context,
      MaterialPageRoute(builder: (context) => const LoginScreen()),
    );
  }

  void _showLogoutConfirmation() {
    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        backgroundColor: AppTheme.cardBg,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
        title: const Text(
          'Đăng xuất',
          style: TextStyle(color: AppTheme.textPrimary, fontWeight: FontWeight.bold),
        ),
        content: const Text(
          'Bạn có chắc chắn muốn đăng xuất khỏi ứng dụng?',
          style: TextStyle(color: AppTheme.textSecondary),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Hủy', style: TextStyle(color: AppTheme.textMuted)),
          ),
          TextButton(
            onPressed: () {
              Navigator.pop(context); // Đóng Dialog
              _logout();
            },
            child: const Text(
              'Đăng xuất',
              style: TextStyle(
                color: AppTheme.error,
                fontWeight: FontWeight.bold,
              ),
            ),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppTheme.background,
      body: SafeArea(
        child: Column(
          children: [
            // Custom Premium Header
            _buildHeader(),

            // Horizontal Filters (Brands & Types)
            _buildFiltersSection(),

            // Product Grid or Loading or Error
            Expanded(
              child: Padding(
                padding: const EdgeInsets.symmetric(horizontal: 16.0),
                child: _buildMainContent(),
              ),
            ),
          ],
        ),
      ),
      floatingActionButton: _showBackToTop
          ? FloatingActionButton(
              onPressed: () {
                _scrollController.animateTo(
                  0,
                  duration: const Duration(milliseconds: 600),
                  curve: Curves.easeInOutCubic,
                );
              },
              backgroundColor: AppTheme.primary,
              foregroundColor: Colors.white,
              tooltip: 'Cuộn lên đầu',
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(16),
              ),
              child: const Icon(Icons.keyboard_arrow_up_rounded, size: 28),
            )
          : null,
    );
  }

  Widget _buildHeader() {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 12),
      decoration: const BoxDecoration(
        color: AppTheme.cardBg,
        border: Border(
          bottom: BorderSide(
            color: AppTheme.border,
            width: 1,
          ),
        ),
      ),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Row(
            children: [
              Semantics(
                label: 'eShop Logo',
                child: SvgPicture.asset(
                  'assets/images/logo-header.svg',
                  width: 44,
                  height: 24,
                ),
              ),
              if (MediaQuery.of(context).size.width > 340) ...[
                const SizedBox(width: 12),
                Semantics(
                  header: true,
                  child: const Text(
                    'Catalog',
                    style: TextStyle(
                      fontSize: 20,
                      fontWeight: FontWeight.bold,
                      color: AppTheme.textPrimary,
                      letterSpacing: 0.5,
                    ),
                  ),
                ),
              ],
            ],
          ),
          Row(
            children: [
              // Shopping Cart Widget Mockup (Tích hợp đếm số lượng thực tế)
              Stack(
                clipBehavior: Clip.none,
                children: [
                  Semantics(
                    label: 'Giỏ hàng',
                    button: true,
                    child: IconButton(
                      icon: const Icon(
                        Icons.shopping_bag_outlined,
                        color: AppTheme.textSecondary,
                        size: 26,
                      ),
                      tooltip: 'Giỏ hàng',
                      onPressed: () async {
                        await Navigator.push(
                          context,
                          MaterialPageRoute(
                            builder: (context) => const BasketScreen(),
                          ),
                        );
                        _loadBasket();
                      },
                    ),
                  ),
                  Positioned(
                    right: 4,
                    top: 4,
                    child: IgnorePointer(
                      child: Container(
                        padding: const EdgeInsets.all(4),
                        decoration: const BoxDecoration(
                          color: AppTheme.primary,
                          shape: BoxShape.circle,
                        ),
                        child: Text(
                          '$_cartCount',
                          style: const TextStyle(
                            fontSize: 10,
                            fontWeight: FontWeight.bold,
                            color: Colors.white,
                          ),
                        ),
                      ),
                    ),
                  ),
                ],
              ),
              const SizedBox(width: 8),
              // Logout Button
              Semantics(
                label: 'Đăng xuất',
                button: true,
                child: IconButton(
                  icon: const Icon(
                    Icons.logout_rounded,
                    color: AppTheme.error,
                    size: 24,
                  ),
                  onPressed: _showLogoutConfirmation,
                  tooltip: 'Đăng xuất',
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildFiltersSection() {
    if (_brands.isEmpty && _types.isEmpty) return const SizedBox.shrink();

    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 16.0, vertical: 12.0),
      child: Row(
        children: [
          // Dropdown chọn Hãng (Brands)
          Expanded(
            child: Container(
              padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 4),
              decoration: BoxDecoration(
                color: AppTheme.cardBg,
                borderRadius: BorderRadius.circular(14),
                border: Border.all(
                  color: AppTheme.border,
                  width: 1.2,
                ),
              ),
              child: DropdownButtonHideUnderline(
                child: DropdownButton<int?>(
                  value: _selectedBrandId,
                  dropdownColor: AppTheme.cardBg,
                  icon: const Icon(
                    Icons.keyboard_arrow_down_rounded,
                    color: AppTheme.primary,
                  ),
                  isExpanded: true,
                  hint: const Text(
                    'Chọn Hãng',
                    style: TextStyle(color: AppTheme.textSecondary, fontSize: 14),
                  ),
                  selectedItemBuilder: (BuildContext context) {
                    return [
                      const DropdownMenuItem<int?>(
                        value: null,
                        child: Text(
                          'Hãng: Tất cả',
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: TextStyle(
                            color: AppTheme.textPrimary,
                            fontSize: 14,
                            fontWeight: FontWeight.w500,
                          ),
                        ),
                      ),
                      ..._brands.map((brand) {
                        return DropdownMenuItem<int?>(
                          value: brand.id,
                          child: Text(
                            'Hãng: ${brand.brand}',
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                            style: const TextStyle(
                              color: AppTheme.textPrimary,
                              fontSize: 14,
                              fontWeight: FontWeight.w500,
                            ),
                          ),
                        );
                      }),
                    ];
                  },
                  items: [
                    const DropdownMenuItem<int?>(
                      value: null,
                      child: Text(
                        'Tất cả Hãng',
                        style: TextStyle(color: AppTheme.textSecondary),
                      ),
                    ),
                    ..._brands.map((brand) {
                      return DropdownMenuItem<int?>(
                        value: brand.id,
                        child: Text(
                          brand.brand,
                          style: const TextStyle(color: AppTheme.textPrimary),
                        ),
                      );
                    }),
                  ],
                  onChanged: _onBrandSelected,
                ),
              ),
            ),
          ),
          const SizedBox(width: 12),
          // Dropdown chọn Loại (Types)
          Expanded(
            child: Container(
              padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 4),
              decoration: BoxDecoration(
                color: AppTheme.cardBg,
                borderRadius: BorderRadius.circular(14),
                border: Border.all(
                  color: AppTheme.border,
                  width: 1.2,
                ),
              ),
              child: DropdownButtonHideUnderline(
                child: DropdownButton<int?>(
                  value: _selectedTypeId,
                  dropdownColor: AppTheme.cardBg,
                  icon: const Icon(
                    Icons.keyboard_arrow_down_rounded,
                    color: AppTheme.primary,
                  ),
                  isExpanded: true,
                  hint: const Text(
                    'Chọn Loại',
                    style: TextStyle(color: AppTheme.textSecondary, fontSize: 14),
                  ),
                  selectedItemBuilder: (BuildContext context) {
                    return [
                      const DropdownMenuItem<int?>(
                        value: null,
                        child: Text(
                          'Loại: Tất cả',
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: TextStyle(
                            color: AppTheme.textPrimary,
                            fontSize: 14,
                            fontWeight: FontWeight.w500,
                          ),
                        ),
                      ),
                      ..._types.map((type) {
                        return DropdownMenuItem<int?>(
                          value: type.id,
                          child: Text(
                            'Loại: ${type.type}',
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                            style: const TextStyle(
                              color: AppTheme.textPrimary,
                              fontSize: 14,
                              fontWeight: FontWeight.w500,
                            ),
                          ),
                        );
                      }),
                    ];
                  },
                  items: [
                    const DropdownMenuItem<int?>(
                      value: null,
                      child: Text(
                        'Tất cả Loại',
                        style: TextStyle(color: AppTheme.textSecondary),
                      ),
                    ),
                    ..._types.map((type) {
                      return DropdownMenuItem<int?>(
                        value: type.id,
                        child: Text(
                          type.type,
                          style: const TextStyle(color: AppTheme.textPrimary),
                        ),
                      );
                    }),
                  ],
                  onChanged: _onTypeSelected,
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildMainContent() {
    if (_isLoading) {
      return const Center(child: CircularProgressIndicator(color: AppTheme.primary));
    }

    if (_error != null) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const Icon(
              Icons.cloud_off_outlined,
              size: 64,
              color: AppTheme.error,
            ),
            const SizedBox(height: 16),
            Text(
              _error!,
              textAlign: TextAlign.center,
              style: const TextStyle(color: AppTheme.textSecondary, fontSize: 15),
            ),
            const SizedBox(height: 24),
            ElevatedButton.icon(
              onPressed: _loadInitialData,
              icon: const Icon(Icons.refresh_rounded),
              label: const Text('Thử lại'),
              style: ElevatedButton.styleFrom(
                backgroundColor: AppTheme.primary,
                foregroundColor: Colors.white,
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(12),
                ),
                padding: const EdgeInsets.symmetric(
                  horizontal: 24,
                  vertical: 12,
                ),
              ),
            ),
          ],
        ),
      );
    }

    if (_items.isEmpty) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const Text(
              'Không tìm thấy sản phẩm nào.',
              style: TextStyle(color: AppTheme.textSecondary, fontSize: 16),
            ),
            if (_filterError != null) ...[
              const SizedBox(height: 16),
              Text(
                _filterError!,
                style: const TextStyle(color: AppTheme.error, fontSize: 14),
              ),
              const SizedBox(height: 16),
              ElevatedButton.icon(
                onPressed: _loadFilteredItems,
                icon: const Icon(Icons.refresh_rounded),
                label: const Text('Thử lại'),
                style: ElevatedButton.styleFrom(
                  backgroundColor: AppTheme.primary,
                  foregroundColor: Colors.white,
                ),
              ),
            ],
          ],
        ),
      );
    }

    // Grid hiển thị sản phẩm dạng nhiều cột responsive tích hợp Pull-To-Refresh & Infinite Scroll
    return LayoutBuilder(
      builder: (context, constraints) {
        final width = constraints.maxWidth;
        int crossAxisCount = 2;
        double childAspectRatio = 0.68;

        if (width >= 900) {
          crossAxisCount = 4;
          childAspectRatio = 0.75;
        } else if (width >= 600) {
          crossAxisCount = 3;
          childAspectRatio = 0.7;
        } else if (width < 340) {
          crossAxisCount = 1;
          childAspectRatio = 0.75;
        }

        return Stack(
          children: [
            RefreshIndicator(
              onRefresh: _loadInitialData,
              color: AppTheme.primary,
              backgroundColor: AppTheme.cardBg,
              child: GridView.builder(
                controller: _scrollController,
                physics: const AlwaysScrollableScrollPhysics(
                  parent: BouncingScrollPhysics(),
                ),
                padding: const EdgeInsets.symmetric(vertical: 16),
                gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
                  crossAxisCount: crossAxisCount,
                  childAspectRatio: childAspectRatio,
                  crossAxisSpacing: 14,
                  mainAxisSpacing: 14,
                ),
                itemCount: _items.length,
                itemBuilder: (context, index) {
                  final item = _items[index];
                  final imageUrl =
                      '${AppConfig.apiBaseUrl}/api/catalog/items/${item.id}/pic?api-version=2.0';

                  return Semantics(
                    label: 'Sản phẩm: ${item.name}, Giá: ${_currencyFormatter.format(item.price)}',
                    child: Container(
                      decoration: BoxDecoration(
                        color: AppTheme.cardBg,
                        borderRadius: BorderRadius.circular(20),
                        border: Border.all(
                          color: AppTheme.border,
                          width: 1,
                        ),
                        boxShadow: [
                          BoxShadow(
                            color: Colors.black.withValues(alpha: 0.02),
                            blurRadius: 10,
                            offset: const Offset(0, 2),
                          ),
                        ],
                      ),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          // Product Image Container
                          Expanded(
                            child: Container(
                              width: double.infinity,
                              color: Colors.transparent,
                              padding: const EdgeInsets.all(12),
                              child: Image.network(
                                imageUrl,
                                fit: BoxFit.contain,
                                loadingBuilder:
                                    (context, child, loadingProgress) {
                                      if (loadingProgress == null) return child;
                                      return const Center(
                                        child: SizedBox(
                                          width: 20,
                                          height: 20,
                                          child: CircularProgressIndicator(
                                            strokeWidth: 2,
                                            color: AppTheme.primary,
                                          ),
                                        ),
                                      );
                                    },
                                errorBuilder: (context, error, stackTrace) {
                                  return const Center(
                                    child: Icon(
                                      Icons.image_not_supported_outlined,
                                      color: AppTheme.textMuted,
                                      size: 36,
                                    ),
                                  );
                                },
                              ),
                            ),
                          ),

                          // Product details
                          Padding(
                            padding: const EdgeInsets.all(12.0),
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                // Brand Pill
                                if (item.catalogBrand != null)
                                  Container(
                                    padding: const EdgeInsets.symmetric(
                                      horizontal: 8,
                                      vertical: 3,
                                    ),
                                    decoration: BoxDecoration(
                                      color: AppTheme.primaryLight,
                                      borderRadius: BorderRadius.circular(8),
                                    ),
                                    child: Text(
                                      item.catalogBrand!.brand,
                                      maxLines: 1,
                                      overflow: TextOverflow.ellipsis,
                                      style: const TextStyle(
                                        color: AppTheme.primary,
                                        fontSize: 10,
                                        fontWeight: FontWeight.bold,
                                      ),
                                    ),
                                  ),
                                const SizedBox(height: 6),
                                // Product Name
                                Text(
                                  item.name,
                                  maxLines: 2,
                                  overflow: TextOverflow.ellipsis,
                                  style: const TextStyle(
                                    color: AppTheme.textPrimary,
                                    fontSize: 14,
                                    fontWeight: FontWeight.bold,
                                  ),
                                ),
                                const SizedBox(height: 8),
                                // Price & Buy Button
                                Row(
                                  mainAxisAlignment:
                                      MainAxisAlignment.spaceBetween,
                                  children: [
                                    Expanded(
                                      child: Text(
                                        _currencyFormatter.format(item.price),
                                        maxLines: 1,
                                        overflow: TextOverflow.ellipsis,
                                        style: const TextStyle(
                                          color: AppTheme.primary,
                                          fontSize: 14,
                                          fontWeight: FontWeight.bold,
                                        ),
                                      ),
                                    ),
                                    const SizedBox(width: 4),
                                    Semantics(
                                      label: 'Thêm vào giỏ hàng',
                                      button: true,
                                      child: Container(
                                        decoration: BoxDecoration(
                                          color: AppTheme.primaryLight,
                                          borderRadius: BorderRadius.circular(12),
                                        ),
                                        child: IconButton(
                                          icon: const Icon(
                                            Icons.add_shopping_cart,
                                            color: AppTheme.primary,
                                            size: 18,
                                          ),
                                          tooltip: 'Thêm vào giỏ hàng',
                                          padding: const EdgeInsets.all(6),
                                          constraints: const BoxConstraints(),
                                          onPressed: () => _addToCart(item),
                                        ),
                                      ),
                                    ),
                                  ],
                                ),
                              ],
                            ),
                          ),
                        ],
                      ),
                    ),
                  );
                },
              ),
            ),

            // Indicator khi đang lọc (Smooth Filtering) để tránh giật lag
            if (_isFiltering)
              Positioned(
                top: 0,
                left: 0,
                right: 0,
                child: Container(
                  height: 4,
                  color: Colors.transparent,
                  child: const LinearProgressIndicator(
                    color: AppTheme.primary,
                    backgroundColor: Colors.transparent,
                  ),
                ),
              ),

            // Floating Indicator tải thêm trang (Infinite Scroll) ở đáy màn hình
            if (_isLoadMoreRunning)
              Positioned(
                bottom: 16,
                left: 0,
                right: 0,
                child: Center(
                  child: ClipRRect(
                    borderRadius: BorderRadius.circular(20),
                    child: BackdropFilter(
                      filter: ImageFilter.blur(sigmaX: 10, sigmaY: 10),
                      child: Container(
                        padding: const EdgeInsets.symmetric(
                          horizontal: 16,
                          vertical: 8,
                        ),
                        decoration: BoxDecoration(
                          color: AppTheme.cardBg.withValues(alpha: 0.9),
                          borderRadius: BorderRadius.circular(20),
                          border: Border.all(
                            color: AppTheme.border,
                          ),
                          boxShadow: [
                            BoxShadow(
                              color: Colors.black.withValues(alpha: 0.05),
                              blurRadius: 10,
                              offset: const Offset(0, 2),
                            ),
                          ],
                        ),
                        child: const Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            SizedBox(
                              width: 16,
                              height: 16,
                              child: CircularProgressIndicator(
                                strokeWidth: 2,
                                color: AppTheme.primary,
                              ),
                            ),
                            SizedBox(width: 10),
                            Text(
                              'Đang tải thêm...',
                              style: TextStyle(
                                color: AppTheme.textPrimary,
                                fontSize: 13,
                                fontWeight: FontWeight.w500,
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                  ),
                ),
              ),
          ],
        );
      },
    );
  }
}
