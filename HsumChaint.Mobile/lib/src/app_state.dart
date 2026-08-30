import 'package:flutter/material.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_riverpod/legacy.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'api_client.dart';

final sharedPreferencesProvider = Provider<SharedPreferences>(
  (ref) => throw StateError('SharedPreferences was not initialized.'),
);

final sessionStoreProvider = Provider<SessionStore>(
  (ref) => SessionStore(ref.watch(sharedPreferencesProvider)),
);

final apiClientProvider = Provider<ApiClient>(
  (ref) => ApiClient(ref.watch(sessionStoreProvider)),
);

final authControllerProvider = ChangeNotifierProvider<AuthController>(
  (ref) => AuthController(
    ref.watch(sessionStoreProvider),
    ref.watch(apiClientProvider),
  ),
);

final localeControllerProvider = ChangeNotifierProvider<LocaleController>(
  (ref) => LocaleController(ref.watch(sharedPreferencesProvider)),
);

String get apiUrl {
  String? value;
  try {
    value = dotenv.env['API_URL']?.trim();
  } catch (_) {
    value = null;
  }
  return value == null || value.isEmpty ? 'http://localhost:5174' : value;
}

class AppText {
  AppText(this.locale);

  final Locale locale;

  bool get isMyanmar => locale.languageCode == 'my';

  String get(String key) {
    return (_strings[locale.languageCode] ?? _strings['my']!)[key] ??
        _strings['en']![key] ??
        key;
  }

  String enumLabel(String group, int value) {
    return get('$group.$value');
  }
}

const _strings = {
  'my': {
    'appTitle': 'ဆွမ်းချိုင့်',
    'login': 'ဝင်ရန်',
    'register': 'စာရင်းသွင်းရန်',
    'logout': 'ထွက်ရန်',
    'phone': 'ဖုန်းနံပါတ်',
    'password': 'စကားဝှက်',
    'name': 'အမည်',
    'email': 'အီးမေးလ်',
    'contactPhone': 'ဆက်သွယ်ရန်ဖုန်း',
    'userType': 'အသုံးပြုသူအမျိုးအစား',
    'monasteryName': 'ကျောင်းအမည်',
    'monasteryAddress': 'ကျောင်းလိပ်စာ',
    'description': 'ဖော်ပြချက်',
    'address': 'လိပ်စာ',
    'save': 'သိမ်းရန်',
    'create': 'ဖန်တီးရန်',
    'cancel': 'ပယ်ဖျက်ရန်',
    'delete': 'ဖျက်ရန်',
    'edit': 'ပြင်ရန်',
    'detail': 'အသေးစိတ်',
    'refresh': 'ပြန်တင်ရန်',
    'monasteries': 'ကျောင်းများ',
    'donations': 'လှူဒါန်းမှု',
    'notifications': 'အသိပေးချက်',
    'users': 'အသုံးပြုသူ',
    'profile': 'ကိုယ်ရေး',
    'members': 'အဖွဲ့ဝင်များ',
    'invite': 'ဖိတ်ရန်',
    'role': 'တာဝန်',
    'status': 'အခြေအနေ',
    'accept': 'လက်ခံရန်',
    'reject': 'ငြင်းရန်',
    'amount': 'ငွေပမာဏ',
    'quantity': 'အရေအတွက်',
    'note': 'မှတ်ချက်',
    'donationType': 'လှူဒါန်းမှုအမျိုးအစား',
    'customType': 'အခြားအမျိုးအစား',
    'pickupTime': 'လာယူမည့်အချိန်',
    'dropoffTime': 'လာပို့မည့်အချိန်',
    'requestDonation': 'လှူဒါန်းရန်တောင်းဆိုမှု',
    'manualDonation': 'လှူဒါန်းမှုမှတ်တမ်း',
    'review': 'စစ်ဆေးရန်',
    'schedule': 'အချိန်သတ်မှတ်ရန်',
    'complete': 'ပြီးစီးရန်',
    'markRead': 'ဖတ်ပြီး',
    'message': 'စာသား',
    'apiUrl': 'API URL',
    'empty': 'ဒေတာမရှိပါ',
    'required': 'လိုအပ်သည်',
    'success': 'အောင်မြင်သည်',
    'failed': 'မအောင်မြင်ပါ',
    'language': 'English',
    'UserType.0': 'အသုံးပြုသူ',
    'UserType.1': 'ရဟန်း',
    'MonasteryRole.0': 'ပိုင်ရှင်',
    'MonasteryRole.1': 'အက်ဒမင်',
    'MonasteryRole.2': 'စာရေး',
    'MonasteryRole.3': 'ကြည့်ရှုသူ',
    'InvitationStatus.0': 'စောင့်ဆိုင်း',
    'InvitationStatus.1': 'လက်ခံ',
    'InvitationStatus.2': 'ငြင်း',
    'DonationType.0': 'ငွေ',
    'DonationType.1': 'အစားအစာ',
    'DonationType.2': 'ဆေးဝါး',
    'DonationType.3': 'သင်္ကန်း',
    'DonationType.4': 'ပစ္စည်း',
    'DonationType.5': 'အခြား',
    'DonationStatus.0': 'မူကြမ်း',
    'DonationStatus.1': 'တင်ပြီး',
    'DonationStatus.2': 'စစ်ဆေးရန်',
    'DonationStatus.3': 'လက်ခံပြီး',
    'DonationStatus.4': 'အချိန်သတ်မှတ်ပြီး',
    'DonationStatus.5': 'ပြီးစီးပြီး',
    'DonationStatus.6': 'ငြင်းပြီး',
    'DonationStatus.7': 'ပယ်ဖျက်ပြီး',
  },
  'en': {
    'appTitle': 'Hsum Chaint',
    'login': 'Login',
    'register': 'Register',
    'logout': 'Logout',
    'phone': 'Phone number',
    'password': 'Password',
    'name': 'Name',
    'email': 'Email',
    'contactPhone': 'Contact phone',
    'userType': 'User type',
    'monasteryName': 'Monastery name',
    'monasteryAddress': 'Monastery address',
    'description': 'Description',
    'address': 'Address',
    'save': 'Save',
    'create': 'Create',
    'cancel': 'Cancel',
    'delete': 'Delete',
    'edit': 'Edit',
    'detail': 'Detail',
    'refresh': 'Refresh',
    'monasteries': 'Monasteries',
    'donations': 'Donations',
    'notifications': 'Notifications',
    'users': 'Users',
    'profile': 'Profile',
    'members': 'Members',
    'invite': 'Invite',
    'role': 'Role',
    'status': 'Status',
    'accept': 'Accept',
    'reject': 'Reject',
    'amount': 'Amount',
    'quantity': 'Quantity',
    'note': 'Note',
    'donationType': 'Donation type',
    'customType': 'Custom type',
    'pickupTime': 'Pickup time',
    'dropoffTime': 'Dropoff time',
    'requestDonation': 'Request donation',
    'manualDonation': 'Manual donation',
    'review': 'Review',
    'schedule': 'Schedule',
    'complete': 'Complete',
    'markRead': 'Mark read',
    'message': 'Message',
    'apiUrl': 'API URL',
    'empty': 'No data',
    'required': 'Required',
    'success': 'Success',
    'failed': 'Failed',
    'language': 'မြန်မာ',
    'UserType.0': 'User',
    'UserType.1': 'Monk',
    'MonasteryRole.0': 'Owner',
    'MonasteryRole.1': 'Admin',
    'MonasteryRole.2': 'Editor',
    'MonasteryRole.3': 'Viewer',
    'InvitationStatus.0': 'Pending',
    'InvitationStatus.1': 'Accept',
    'InvitationStatus.2': 'Reject',
    'DonationType.0': 'Money',
    'DonationType.1': 'Food',
    'DonationType.2': 'Medicine',
    'DonationType.3': 'Robe',
    'DonationType.4': 'Supplies',
    'DonationType.5': 'Other',
    'DonationStatus.0': 'Draft',
    'DonationStatus.1': 'Submitted',
    'DonationStatus.2': 'Pending review',
    'DonationStatus.3': 'Accepted',
    'DonationStatus.4': 'Scheduled',
    'DonationStatus.5': 'Completed',
    'DonationStatus.6': 'Rejected',
    'DonationStatus.7': 'Cancelled',
  },
};

class LocaleController extends ChangeNotifier {
  LocaleController(this._preferences)
    : locale = Locale(_preferences.getString(_localeKey) ?? 'my');

  static const _localeKey = 'locale';
  final SharedPreferences _preferences;
  Locale locale;

  Future<void> toggle() async {
    locale = Locale(locale.languageCode == 'my' ? 'en' : 'my');
    await _preferences.setString(_localeKey, locale.languageCode);
    notifyListeners();
  }
}

class AuthSession {
  const AuthSession({
    this.accessToken,
    this.refreshToken,
    this.userId,
    this.userType,
  });

  final String? accessToken;
  final String? refreshToken;
  final int? userId;
  final int? userType;

  bool get isAuthenticated =>
      accessToken != null && refreshToken != null && userId != null;
}

class SessionStore {
  SessionStore(this._preferences);

  static const _accessTokenKey = 'accessToken';
  static const _refreshTokenKey = 'refreshToken';
  static const _userIdKey = 'userId';
  static const _userTypeKey = 'userType';

  final SharedPreferences _preferences;

  AuthSession load() {
    return AuthSession(
      accessToken: _preferences.getString(_accessTokenKey),
      refreshToken: _preferences.getString(_refreshTokenKey),
      userId: _preferences.getInt(_userIdKey),
      userType: _preferences.getInt(_userTypeKey),
    );
  }

  Future<void> save(AuthSession session) async {
    if (session.accessToken != null) {
      await _preferences.setString(_accessTokenKey, session.accessToken!);
    }
    if (session.refreshToken != null) {
      await _preferences.setString(_refreshTokenKey, session.refreshToken!);
    }
    if (session.userId != null) {
      await _preferences.setInt(_userIdKey, session.userId!);
    }
    if (session.userType != null) {
      await _preferences.setInt(_userTypeKey, session.userType!);
    }
  }

  Future<void> clear() async {
    await _preferences.remove(_accessTokenKey);
    await _preferences.remove(_refreshTokenKey);
    await _preferences.remove(_userIdKey);
    await _preferences.remove(_userTypeKey);
  }
}

class AuthController extends ChangeNotifier {
  AuthController(this._store, this._api) {
    session = _store.load();
    isReady = true;
    _api.onSessionRefreshed = _applySession;
    _api.onSessionExpired = logout;
  }

  final SessionStore _store;
  final ApiClient _api;

  AuthSession session = const AuthSession();
  bool isReady = false;
  bool isBusy = false;
  String? message;

  Future<bool> login(String phoneNumber, String password) async {
    return _run(() async {
      final response = await _api.login(phoneNumber, password);
      final data = response.dataAsMap;
      await _applySession(
        AuthSession(
          accessToken: data.stringValue('accessToken'),
          refreshToken: data.stringValue('refreshToken'),
          userId: data.intValue('id'),
          userType: data.intValue('userType'),
        ),
      );
      message = response.message;
    });
  }

  Future<bool> register(Map<String, dynamic> body) async {
    return _run(() async {
      final response = await _api.register(body);
      message = response.message.isEmpty
          ? 'Register successful'
          : response.message;
    });
  }

  Future<void> logout() async {
    await _store.clear();
    session = const AuthSession();
    notifyListeners();
  }

  Future<bool> _run(Future<void> Function() action) async {
    isBusy = true;
    message = null;
    notifyListeners();
    try {
      await action();
      return true;
    } on ApiException catch (error) {
      message = error.message;
      return false;
    } finally {
      isBusy = false;
      notifyListeners();
    }
  }

  Future<void> _applySession(AuthSession next) async {
    session = next;
    await _store.save(next);
    notifyListeners();
  }
}

extension JsonMapRead on Map<String, dynamic> {
  String? stringValue(String key) {
    final value = this[key] ?? this[_pascal(key)];
    return value?.toString();
  }

  int? intValue(String key) {
    final value = this[key] ?? this[_pascal(key)];
    if (value is int) return value;
    if (value is num) return value.toInt();
    return int.tryParse(value?.toString() ?? '');
  }

  static String _pascal(String key) =>
      key.isEmpty ? key : key[0].toUpperCase() + key.substring(1);
}
