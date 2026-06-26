import 'package:flutter/material.dart';
import 'package:api_ordering/api_ordering.dart';
import 'package:api_basket/api_basket.dart' as basket;
import 'package:core_network/core_network.dart';
import '../session_state.dart';
import '../theme.dart';
import '../config.dart';

class CheckoutScreen extends StatefulWidget {
  final List<basket.BasketItem> basketItems;
  final double totalAmount;

  const CheckoutScreen({
    super.key,
    required this.basketItems,
    required this.totalAmount,
  });

  @override
  State<CheckoutScreen> createState() => _CheckoutScreenState();
}

class _CheckoutScreenState extends State<CheckoutScreen> {
  final _orderingApi = OrderingApi(
    NetworkClient(baseUrl: AppConfig.apiBaseUrl, getToken: () async => SessionState.token),
  );

  final _basketApi = basket.BasketApi(
    NetworkClient(baseUrl: AppConfig.apiBaseUrl, getToken: () async => SessionState.token),
  );

  final _formKey = GlobalKey<FormState>();

  // Shipping Address controllers
  final _cityController = TextEditingController(text: 'Hà Nội');
  final _streetController = TextEditingController(text: '123 Đường Láng');
  final _stateController = TextEditingController(text: 'Đống Đa');
  final _countryController = TextEditingController(text: 'Việt Nam');
  final _zipCodeController = TextEditingController(text: '10000');

  // Credit Card controllers
  final _cardNumberController = TextEditingController(text: '4111111111111111');
  final _cardHolderNameController = TextEditingController(text: 'ALICE SMITH');
  final _cardSecurityController = TextEditingController(text: '123');
  DateTime _cardExpiration = DateTime.now().add(const Duration(days: 365));

  List<CardType> _cardTypes = [];
  int? _selectedCardTypeId;
  bool _isPlacingOrder = false;

  @override
  void initState() {
    super.initState();
    _loadCardTypes();
  }

  @override
  void dispose() {
    _cityController.dispose();
    _streetController.dispose();
    _stateController.dispose();
    _countryController.dispose();
    _zipCodeController.dispose();
    _cardNumberController.dispose();
    _cardHolderNameController.dispose();
    _cardSecurityController.dispose();
    super.dispose();
  }

  Future<void> _loadCardTypes() async {
    try {
      final cardTypes = await _orderingApi.getCardTypes();
      setState(() {
        _cardTypes = cardTypes;
        if (cardTypes.isNotEmpty) {
          _selectedCardTypeId = cardTypes.first.id;
        }
      });
    } catch (e) {
      // Dùng danh sách loại thẻ mặc định nếu lỗi API
      setState(() {
        _cardTypes = [
          CardType(id: 1, name: 'Visa'),
          CardType(id: 2, name: 'MasterCard'),
        ];
        _selectedCardTypeId = 1;
      });
    }
  }

  Future<void> _placeOrder() async {
    if (_formKey.currentState?.validate() != true) return;

    setState(() {
      _isPlacingOrder = true;
    });

    try {
      final orderItems = widget.basketItems
          .map(
            (item) => BasketItemRequest(
              id: item.id ?? '',
              productId: item.productId,
              productName: item.productName ?? '',
              unitPrice: item.unitPrice ?? 0.0,
              oldUnitPrice: item.oldUnitPrice ?? 0.0,
              quantity: item.quantity,
              pictureUrl: item.pictureUrl ?? '',
            ),
          )
          .toList();

      final request = CreateOrderRequest(
        userId: SessionState.userId ?? 'alice',
        userName: SessionState.userName ?? 'alice@eshop.com',
        city: _cityController.text.trim(),
        street: _streetController.text.trim(),
        state: _stateController.text.trim(),
        country: _countryController.text.trim(),
        zipCode: _zipCodeController.text.trim(),
        cardNumber: _cardNumberController.text.trim(),
        cardHolderName: _cardHolderNameController.text.trim().toUpperCase(),
        cardExpiration: _cardExpiration,
        cardSecurityNumber: _cardSecurityController.text.trim(),
        cardTypeId: _selectedCardTypeId ?? 1,
        buyer: SessionState.userId ?? 'alice',
        items: orderItems,
      );

      // Gọi API đặt hàng của dịch vụ Ordering
      await _orderingApi.createOrder(request);

      // Xóa sạch giỏ hàng của dịch vụ Basket sau khi đặt hàng thành công
      await _basketApi.deleteBasket();

      setState(() {
        _isPlacingOrder = false;
      });

      if (mounted) {
        _showSuccessDialog();
      }
    } catch (e) {
      setState(() {
        _isPlacingOrder = false;
      });
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Lỗi khi đặt hàng: $e'),
            behavior: SnackBarBehavior.floating,
          ),
        );
      }
    }
  }

  void _showSuccessDialog() {
    showDialog(
      context: context,
      barrierDismissible: false,
      builder: (context) => AlertDialog(
        backgroundColor: AppTheme.cardBg,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const SizedBox(height: 16),
            const Icon(
              Icons.check_circle_outline_rounded,
              color: AppTheme.success,
              size: 72,
            ),
            const SizedBox(height: 20),
            const Text(
              'Đặt hàng thành công!',
              style: TextStyle(
                color: AppTheme.textPrimary,
                fontSize: 20,
                fontWeight: FontWeight.bold,
              ),
            ),
            const SizedBox(height: 12),
            const Text(
              'Đơn hàng của bạn đã được tiếp nhận và xử lý. Cảm ơn bạn đã mua sắm tại eShop!',
              textAlign: TextAlign.center,
              style: TextStyle(color: AppTheme.textSecondary, fontSize: 14),
            ),
            const SizedBox(height: 24),
            SizedBox(
              width: double.infinity,
              height: 48,
              child: ElevatedButton(
                onPressed: () {
                  Navigator.pop(context); // Đóng Dialog
                  Navigator.pop(
                    context,
                    true,
                  ); // Thoát Checkout và trả về true để xóa giỏ hàng UI
                },
                style: ElevatedButton.styleFrom(
                  backgroundColor: AppTheme.primary,
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(12),
                  ),
                ),
                child: const Text(
                  'Quay lại Cửa hàng',
                  style: TextStyle(
                    color: Colors.white,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ),
            ),
          ],
        ),
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
            // Header
            _buildHeader(),

            // Form nhập thông tin thanh toán & địa chỉ
            Expanded(
              child: SingleChildScrollView(
                padding: const EdgeInsets.all(20),
                child: Form(
                  key: _formKey,
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      _buildSectionTitle('1. Địa chỉ nhận hàng'),
                      const SizedBox(height: 12),
                      _buildAddressForm(),
                      const SizedBox(height: 24),
                      _buildSectionTitle('2. Thông tin thanh toán'),
                      const SizedBox(height: 12),
                      _buildPaymentForm(),
                      const SizedBox(height: 36),

                      // Nút đặt hàng
                      Semantics(
                        label: 'Đặt hàng ngay',
                        button: true,
                        child: SizedBox(
                          width: double.infinity,
                          height: 54,
                          child: ElevatedButton(
                            onPressed: _isPlacingOrder ? null : _placeOrder,
                            style: ElevatedButton.styleFrom(
                              backgroundColor: AppTheme.primary,
                              shape: RoundedRectangleBorder(
                                borderRadius: BorderRadius.circular(16),
                              ),
                            ),
                            child: _isPlacingOrder
                                ? const SizedBox(
                                    width: 24,
                                    height: 24,
                                    child: CircularProgressIndicator(
                                      color: Colors.white,
                                      strokeWidth: 2.5,
                                    ),
                                  )
                                : const Text(
                                    'Đặt hàng ngay',
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
              ),
            ),
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
              onPressed: () => Navigator.pop(context),
            ),
          ),
          const SizedBox(width: 8),
          Semantics(
            header: true,
            child: const Text(
              'Thanh toán',
              style: TextStyle(
                fontSize: 20,
                fontWeight: FontWeight.bold,
                color: AppTheme.textPrimary,
                letterSpacing: 0.5,
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildSectionTitle(String title) {
    return Text(
      title,
      style: const TextStyle(
        color: AppTheme.textPrimary,
        fontSize: 16,
        fontWeight: FontWeight.bold,
      ),
    );
  }

  Widget _buildAddressForm() {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: AppTheme.cardBg,
        borderRadius: BorderRadius.circular(20),
        border: Border.all(color: AppTheme.border),
      ),
      child: Column(
        children: [
          _buildTextField(
            controller: _streetController,
            label: 'Đường/Số nhà',
            icon: Icons.home_outlined,
            validator: (val) =>
                val == null || val.isEmpty ? 'Không được để trống!' : null,
          ),
          const SizedBox(height: 14),
          Row(
            children: [
              Expanded(
                child: _buildTextField(
                  controller: _cityController,
                  label: 'Thành phố',
                  icon: Icons.location_city_outlined,
                  validator: (val) => val == null || val.isEmpty
                      ? 'Không được để trống!'
                      : null,
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: _buildTextField(
                  controller: _stateController,
                  label: 'Quận/Huyện',
                  icon: Icons.map_outlined,
                  validator: (val) => val == null || val.isEmpty
                      ? 'Không được để trống!'
                      : null,
                ),
              ),
            ],
          ),
          const SizedBox(height: 14),
          Row(
            children: [
              Expanded(
                child: _buildTextField(
                  controller: _countryController,
                  label: 'Quốc gia',
                  icon: Icons.public_outlined,
                  validator: (val) => val == null || val.isEmpty
                      ? 'Không được để trống!'
                      : null,
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: _buildTextField(
                  controller: _zipCodeController,
                  label: 'Mã bưu điện (Zip)',
                  icon: Icons.markunread_mailbox_outlined,
                  validator: (val) => val == null || val.isEmpty
                      ? 'Không được để trống!'
                      : null,
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildPaymentForm() {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: AppTheme.cardBg,
        borderRadius: BorderRadius.circular(20),
        border: Border.all(color: AppTheme.border),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _buildTextField(
            controller: _cardHolderNameController,
            label: 'Tên chủ thẻ',
            icon: Icons.person_outline,
            validator: (val) =>
                val == null || val.isEmpty ? 'Không được để trống!' : null,
          ),
          const SizedBox(height: 14),
          _buildTextField(
            controller: _cardNumberController,
            label: 'Số thẻ thanh toán',
            icon: Icons.credit_card_outlined,
            keyboardType: TextInputType.number,
            validator: (val) => val == null || val.length < 16
                ? 'Số thẻ phải chứa ít nhất 16 số!'
                : null,
          ),
          const SizedBox(height: 14),
          Row(
            children: [
              Expanded(
                child: _buildTextField(
                  controller: _cardSecurityController,
                  label: 'Mã CVV',
                  icon: Icons.security_outlined,
                  keyboardType: TextInputType.number,
                  validator: (val) =>
                      val == null || val.length < 3 ? 'Cần 3 chữ số!' : null,
                ),
              ),
              const SizedBox(width: 12),
              Expanded(child: _buildCardTypeDropdown()),
            ],
          ),
          const SizedBox(height: 14),
          _buildExpirationDatePicker(),
        ],
      ),
    );
  }

  Widget _buildTextField({
    required TextEditingController controller,
    required String label,
    required IconData icon,
    TextInputType keyboardType = TextInputType.text,
    String? Function(String?)? validator,
  }) {
    return TextFormField(
      controller: controller,
      style: const TextStyle(color: AppTheme.textPrimary, fontSize: 14),
      keyboardType: keyboardType,
      validator: validator,
      decoration: InputDecoration(
        prefixIcon: Icon(icon, color: AppTheme.textSecondary, size: 18),
        labelText: label,
        labelStyle: const TextStyle(color: AppTheme.textSecondary, fontSize: 13),
        filled: true,
        fillColor: AppTheme.background,
        contentPadding: const EdgeInsets.symmetric(
          vertical: 14,
          horizontal: 16,
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: const BorderSide(color: AppTheme.border),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: const BorderSide(color: AppTheme.primary, width: 1.5),
        ),
        errorBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: const BorderSide(color: AppTheme.error),
        ),
        focusedErrorBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: const BorderSide(color: AppTheme.error, width: 1.5),
        ),
      ),
    );
  }

  Widget _buildCardTypeDropdown() {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
      decoration: BoxDecoration(
        color: AppTheme.background,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: AppTheme.border),
      ),
      child: DropdownButtonHideUnderline(
        child: DropdownButton<int>(
          value: _selectedCardTypeId,
          dropdownColor: AppTheme.cardBg,
          icon: const Icon(
            Icons.keyboard_arrow_down_rounded,
            color: AppTheme.textSecondary,
          ),
          isExpanded: true,
          isDense: true,
          hint: const Text(
            'Loại thẻ',
            style: TextStyle(color: AppTheme.textSecondary, fontSize: 13),
            overflow: TextOverflow.ellipsis,
            maxLines: 1,
          ),
          items: _cardTypes.map((type) {
            return DropdownMenuItem<int>(
              value: type.id,
              child: Text(
                type.name,
                style: const TextStyle(color: AppTheme.textPrimary, fontSize: 14),
                overflow: TextOverflow.ellipsis,
                maxLines: 1,
              ),
            );
          }).toList(),
          onChanged: (val) {
            if (val != null) {
              setState(() => _selectedCardTypeId = val);
            }
          },
        ),
      ),
    );
  }

  Widget _buildExpirationDatePicker() {
    final expiryStr =
        "${_cardExpiration.month.toString().padLeft(2, '0')}/${_cardExpiration.year}";

    return InkWell(
      onTap: () async {
        final picked = await showDatePicker(
          context: context,
          initialDate: _cardExpiration,
          firstDate: DateTime.now(),
          lastDate: DateTime.now().add(const Duration(days: 3650)),
          builder: (context, child) {
            return Theme(
              data: Theme.of(context).copyWith(
                colorScheme: const ColorScheme.light(
                  primary: AppTheme.primary,
                  onPrimary: Colors.white,
                  surface: AppTheme.cardBg,
                  onSurface: AppTheme.textPrimary,
                ),
              ),
              child: child!,
            );
          },
        );

        if (picked != null) {
          setState(() {
            _cardExpiration = picked;
          });
        }
      },
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
        decoration: BoxDecoration(
          color: AppTheme.background,
          borderRadius: BorderRadius.circular(12),
          border: Border.all(color: AppTheme.border),
        ),
        child: Row(
          children: [
            const Icon(
              Icons.calendar_month_outlined,
              color: AppTheme.textSecondary,
              size: 18,
            ),
            const SizedBox(width: 12),
            Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text(
                  'Hạn hết hạn thẻ',
                  style: TextStyle(color: AppTheme.textSecondary, fontSize: 10),
                ),
                const SizedBox(height: 2),
                Text(
                  expiryStr,
                  style: const TextStyle(
                    color: AppTheme.textPrimary,
                    fontSize: 14,
                    fontWeight: FontWeight.w500,
                  ),
                ),
              ],
            ),
            const Spacer(),
            const Icon(Icons.arrow_drop_down, color: AppTheme.textSecondary),
          ],
        ),
      ),
    );
  }
}
