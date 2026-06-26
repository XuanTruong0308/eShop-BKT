import 'package:flutter/material.dart';
import 'package:api_basket/api_basket.dart';
import 'package:core_network/core_network.dart';
import 'package:intl/intl.dart';
import 'checkout_screen.dart';
import '../session_state.dart';
import '../theme.dart';
import '../config.dart';

class BasketScreen extends StatefulWidget {
  const BasketScreen({super.key});

  @override
  State<BasketScreen> createState() => _BasketScreenState();
}

class _BasketScreenState extends State<BasketScreen> {
  final _basketApi = BasketApi(
    NetworkClient(baseUrl: AppConfig.apiBaseUrl, getToken: () async => SessionState.token),
  );

  final _currencyFormatter = NumberFormat.simpleCurrency(name: 'USD');
  bool _isLoading = true;
  String? _error;
  CustomerBasket? _basket;

  @override
  void initState() {
    super.initState();
    _loadBasket();
  }

  Future<void> _loadBasket() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });

    try {
      final basket = await _basketApi.getBasket();
      setState(() {
        _basket = basket;
        _isLoading = false;
      });
    } catch (e) {
      setState(() {
        _error = 'Không thể lấy thông tin giỏ hàng. Vui lòng thử lại!';
        _isLoading = false;
      });
    }
  }

  Future<void> _updateQuantity(BasketItem item, int newQuantity) async {
    if (newQuantity < 1) {
      // Hỏi người dùng có muốn xóa sản phẩm khỏi giỏ hàng không
      final confirm = await showDialog<bool>(
        context: context,
        builder: (context) => AlertDialog(
          backgroundColor: AppTheme.cardBg,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(16),
          ),
          title: const Text(
            'Xóa sản phẩm',
            style: TextStyle(color: AppTheme.textPrimary, fontWeight: FontWeight.bold),
          ),
          content: const Text(
            'Bạn có chắc chắn muốn xóa sản phẩm này khỏi giỏ hàng?',
            style: TextStyle(color: AppTheme.textSecondary),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context, false),
              child: const Text('Hủy', style: TextStyle(color: AppTheme.textMuted)),
            ),
            TextButton(
              onPressed: () => Navigator.pop(context, true),
              child: const Text(
                'Xóa',
                style: TextStyle(
                  color: AppTheme.error,
                  fontWeight: FontWeight.bold,
                ),
              ),
            ),
          ],
        ),
      );

      if (confirm != true) return;
    }

    setState(() {
      _isLoading = true;
    });

    try {
      final currentItems = List<BasketItem>.from(_basket?.items ?? []);
      if (newQuantity < 1) {
        currentItems.removeWhere((x) => x.productId == item.productId);
      } else {
        final index = currentItems.indexWhere(
          (x) => x.productId == item.productId,
        );
        if (index != -1) {
          currentItems[index] = BasketItem(
            id: item.id,
            productId: item.productId,
            productName: item.productName,
            unitPrice: item.unitPrice,
            oldUnitPrice: item.oldUnitPrice,
            quantity: newQuantity,
            pictureUrl: item.pictureUrl,
          );
        }
      }

      final updatedBasket = await _basketApi.updateBasket(
        CustomerBasket(
          buyerId: _basket?.buyerId ?? 'alice',
          items: currentItems,
        ),
      );

      setState(() {
        _basket = updatedBasket;
        _isLoading = false;
      });
    } catch (e) {
      setState(() {
        _isLoading = false;
      });
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Lỗi khi cập nhật số lượng sản phẩm!'),
            behavior: SnackBarBehavior.floating,
          ),
        );
      }
    }
  }

  double _calculateTotal() {
    double total = 0.0;
    if (_basket != null) {
      for (final item in _basket!.items) {
        total += (item.unitPrice ?? 0.0) * item.quantity;
      }
    }
    return total;
  }

  @override
  Widget build(BuildContext context) {
    final double totalAmount = _calculateTotal();

    return Scaffold(
      backgroundColor: AppTheme.background,
      body: SafeArea(
        child: Column(
          children: [
            // Header
            _buildHeader(),

            // Main body
            Expanded(child: _buildMainContent(totalAmount)),
          ],
        ),
      ),
    );
  }

  Widget _buildHeader() {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
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
        children: [
          Semantics(
            label: 'Quay lại',
            button: true,
            child: IconButton(
              icon: const Icon(
                Icons.arrow_back_ios_new_rounded,
                color: AppTheme.textPrimary,
                size: 20,
              ),
              tooltip: 'Quay lại',
              onPressed: () => Navigator.pop(context, _basket?.items.length ?? 0),
            ),
          ),
          const SizedBox(width: 8),
          Semantics(
            header: true,
            child: const Text(
              'Giỏ Hàng',
              style: TextStyle(
                fontSize: 20,
                fontWeight: FontWeight.bold,
                color: AppTheme.textPrimary,
                letterSpacing: 0.5,
              ),
            ),
          ),
          const Spacer(),
          if (_basket != null && _basket!.items.isNotEmpty)
            Semantics(
              label: 'Xóa sạch giỏ hàng',
              button: true,
              child: IconButton(
                icon: const Icon(
                  Icons.delete_sweep_outlined,
                  color: AppTheme.error,
                  size: 24,
                ),
                tooltip: 'Xóa tất cả',
                onPressed: _showClearBasketConfirmation,
              ),
            ),
        ],
      ),
    );
  }

  void _showClearBasketConfirmation() async {
    final confirm = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        backgroundColor: AppTheme.cardBg,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
        title: const Text(
          'Xóa giỏ hàng',
          style: TextStyle(color: AppTheme.textPrimary, fontWeight: FontWeight.bold),
        ),
        content: const Text(
          'Bạn có chắc chắn muốn xóa toàn bộ sản phẩm khỏi giỏ hàng?',
          style: TextStyle(color: AppTheme.textSecondary),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Hủy', style: TextStyle(color: AppTheme.textMuted)),
          ),
          TextButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text(
              'Xóa tất cả',
              style: TextStyle(
                color: AppTheme.error,
                fontWeight: FontWeight.bold,
              ),
            ),
          ),
        ],
      ),
    );

    if (confirm == true) {
      setState(() => _isLoading = true);
      try {
        await _basketApi.deleteBasket();
        setState(() {
          _basket = CustomerBasket(
            buyerId: _basket?.buyerId ?? 'alice',
            items: [],
          );
          _isLoading = false;
        });
      } catch (e) {
        setState(() => _isLoading = false);
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(
              content: Text('Lỗi khi xóa sạch giỏ hàng!'),
              behavior: SnackBarBehavior.floating,
            ),
          );
        }
      }
    }
  }

  Widget _buildMainContent(double totalAmount) {
    if (_isLoading && _basket == null) {
      return const Center(child: CircularProgressIndicator(color: AppTheme.primary));
    }

    if (_error != null) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const Icon(Icons.error_outline, size: 64, color: AppTheme.error),
            const SizedBox(height: 16),
            Text(
              _error!,
              style: const TextStyle(color: AppTheme.textSecondary, fontSize: 15),
            ),
            const SizedBox(height: 20),
            ElevatedButton(
              onPressed: _loadBasket,
              style: ElevatedButton.styleFrom(
                backgroundColor: AppTheme.primary,
              ),
              child: const Text(
                'Thử lại',
                style: TextStyle(color: Colors.white),
              ),
            ),
          ],
        ),
      );
    }

    if (_basket == null || _basket!.items.isEmpty) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const Icon(
              Icons.shopping_cart_outlined,
              size: 80,
              color: AppTheme.textMuted,
            ),
            const SizedBox(height: 16),
            const Text(
              'Giỏ hàng của bạn đang trống!',
              style: TextStyle(
                color: AppTheme.textSecondary,
                fontSize: 16,
                fontWeight: FontWeight.w500,
              ),
            ),
            const SizedBox(height: 24),
            ElevatedButton(
              onPressed: () => Navigator.pop(context, 0),
              style: ElevatedButton.styleFrom(
                backgroundColor: AppTheme.primary,
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(12),
                ),
                padding: const EdgeInsets.symmetric(
                  horizontal: 24,
                  vertical: 12,
                ),
              ),
              child: const Text(
                'Tiếp tục mua sắm',
                style: TextStyle(
                  color: Colors.white,
                  fontWeight: FontWeight.bold,
                ),
              ),
            ),
          ],
        ),
      );
    }

    return Column(
      children: [
        // Danh sách sản phẩm
        Expanded(
          child: ListView.builder(
            padding: const EdgeInsets.all(16),
            itemCount: _basket!.items.length,
            itemBuilder: (context, index) {
              final item = _basket!.items[index];
              final imageUrl =
                  '${AppConfig.apiBaseUrl}/api/catalog/items/${item.productId}/pic?api-version=2.0';

              return Semantics(
                label: 'Sản phẩm: ${item.productName}, Số lượng: ${item.quantity}',
                child: Container(
                  margin: const EdgeInsets.only(bottom: 14),
                  padding: const EdgeInsets.all(12),
                  decoration: BoxDecoration(
                    color: AppTheme.cardBg,
                    borderRadius: BorderRadius.circular(16),
                    border: Border.all(
                      color: AppTheme.border,
                    ),
                    boxShadow: [
                      BoxShadow(
                        color: Colors.black.withValues(alpha: 0.02),
                        blurRadius: 10,
                        offset: const Offset(0, 2),
                      ),
                    ],
                  ),
                  child: Row(
                    children: [
                      // Ảnh sản phẩm
                      ClipRRect(
                        borderRadius: BorderRadius.circular(12),
                        child: Container(
                          width: 72,
                          height: 72,
                          color: Colors.transparent,
                          child: Image.network(
                            imageUrl,
                            fit: BoxFit.contain,
                            errorBuilder: (context, e, s) => const Icon(
                              Icons.image_not_supported_outlined,
                              color: AppTheme.textMuted,
                            ),
                          ),
                        ),
                      ),
                      const SizedBox(width: 14),

                      // Tên và Giá
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              item.productName ?? 'Sản phẩm #${item.productId}',
                              maxLines: 2,
                              overflow: TextOverflow.ellipsis,
                              style: const TextStyle(
                                color: AppTheme.textPrimary,
                                fontSize: 14,
                                fontWeight: FontWeight.bold,
                              ),
                            ),
                            const SizedBox(height: 6),
                            Text(
                              _currencyFormatter.format(item.unitPrice ?? 0.0),
                              style: const TextStyle(
                                color: AppTheme.primary,
                                fontSize: 14,
                                fontWeight: FontWeight.w600,
                              ),
                            ),
                          ],
                        ),
                      ),
                      const SizedBox(width: 8),

                      // Nút tăng/giảm số lượng (Touch Target an toàn >= 44px)
                      Row(
                        children: [
                          _buildQuantityButton(
                            icon: Icons.remove,
                            label: 'Giảm số lượng',
                            onPressed: () =>
                                _updateQuantity(item, item.quantity - 1),
                          ),
                          Container(
                            constraints: const BoxConstraints(minWidth: 32),
                            alignment: Alignment.center,
                            child: Text(
                              '${item.quantity}',
                              style: const TextStyle(
                                color: AppTheme.textPrimary,
                                fontSize: 15,
                                fontWeight: FontWeight.bold,
                              ),
                            ),
                          ),
                          _buildQuantityButton(
                            icon: Icons.add,
                            label: 'Tăng số lượng',
                            onPressed: () =>
                                _updateQuantity(item, item.quantity + 1),
                          ),
                        ],
                      ),
                    ],
                  ),
                ),
              );
            },
          ),
        ),

        // Thanh toán Bottom Panel
        Container(
          padding: const EdgeInsets.all(20),
          decoration: const BoxDecoration(
            color: AppTheme.cardBg,
            border: Border(
              top: BorderSide(color: AppTheme.border),
            ),
          ),
          child: Column(
            children: [
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  const Text(
                    'Tổng thanh toán:',
                    style: TextStyle(color: AppTheme.textSecondary, fontSize: 15),
                  ),
                  Text(
                    _currencyFormatter.format(totalAmount),
                    style: const TextStyle(
                      color: AppTheme.primary,
                      fontSize: 20,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 16),
              Semantics(
                label: 'Tiến hành Thanh toán',
                button: true,
                child: SizedBox(
                  width: double.infinity,
                  height: 52,
                  child: ElevatedButton(
                    onPressed: () async {
                      final orderPlaced = await Navigator.push<bool>(
                        context,
                        MaterialPageRoute(
                          builder: (context) => CheckoutScreen(
                            basketItems: _basket!.items,
                            totalAmount: totalAmount,
                          ),
                        ),
                      );

                      if (orderPlaced == true) {
                        setState(() {
                          _basket = CustomerBasket(
                            buyerId: _basket?.buyerId ?? 'alice',
                            items: [],
                          );
                        });
                      }
                    },
                    style: ElevatedButton.styleFrom(
                      backgroundColor: AppTheme.primary,
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(16),
                      ),
                    ),
                    child: const Text(
                      'Tiến hành Thanh toán',
                      style: TextStyle(
                        color: Colors.white,
                        fontSize: 16,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                  ),
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }

  Widget _buildQuantityButton({
    required IconData icon,
    required String label,
    required VoidCallback onPressed,
  }) {
    return Semantics(
      label: label,
      button: true,
      child: Container(
        width: 44,
        height: 44,
        decoration: BoxDecoration(
          color: AppTheme.primaryLight,
          borderRadius: BorderRadius.circular(12),
        ),
        child: IconButton(
          icon: Icon(icon, color: AppTheme.primary, size: 16),
          padding: EdgeInsets.zero,
          onPressed: onPressed,
        ),
      ),
    );
  }
}
