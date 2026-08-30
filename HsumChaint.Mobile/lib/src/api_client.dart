import 'package:dio/dio.dart';

import 'app_state.dart';

typedef SessionCallback = Future<void> Function(AuthSession session);

class ApiClient {
  ApiClient(this._store)
    : _dio = Dio(
        BaseOptions(
          baseUrl: apiUrl,
          connectTimeout: const Duration(seconds: 12),
          receiveTimeout: const Duration(seconds: 20),
          sendTimeout: const Duration(seconds: 20),
          headers: {'Content-Type': 'application/json'},
        ),
      ) {
    _dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: (options, handler) {
          final token = _store.load().accessToken;
          if (token != null && token.isNotEmpty) {
            options.headers['Authorization'] = 'Bearer $token';
          }
          handler.next(options);
        },
        onError: (error, handler) async {
          final canRefresh =
              error.response?.statusCode == 401 &&
              error.requestOptions.path != '/api/v1/Auth/refresh-token' &&
              _store.load().refreshToken != null;

          if (!canRefresh) {
            handler.next(error);
            return;
          }

          try {
            final refreshed = await refreshToken();
            await onSessionRefreshed?.call(refreshed);
            final retry = await _dio.fetch<dynamic>(
              error.requestOptions
                ..headers['Authorization'] = 'Bearer ${refreshed.accessToken}',
            );
            handler.resolve(retry);
          } catch (_) {
            await onSessionExpired?.call();
            handler.next(error);
          }
        },
      ),
    );
  }

  final SessionStore _store;
  final Dio _dio;

  SessionCallback? onSessionRefreshed;
  Future<void> Function()? onSessionExpired;

  Future<ApiEnvelope> login(String phoneNumber, String password) {
    return post('/api/v1/Auth/login', {
      'phoneNumber': phoneNumber,
      'password': password,
    });
  }

  Future<ApiEnvelope> register(Map<String, dynamic> body) {
    return post('/api/v1/Auth/register', body);
  }

  Future<AuthSession> refreshToken() async {
    final session = _store.load();
    final response = await post('/api/v1/Auth/refresh-token', {
      'userId': session.userId,
      'refreshToken': session.refreshToken,
    });
    final data = response.dataAsMap;
    return AuthSession(
      accessToken: data.stringValue('accessToken'),
      refreshToken: data.stringValue('refreshToken'),
      userId: data.intValue('id'),
      userType: data.intValue('userType'),
    );
  }

  Future<ApiEnvelope> getUsers() => get('/api/User');
  Future<ApiEnvelope> getUser(int id) => get('/api/User/$id');
  Future<ApiEnvelope> updateUser(Map<String, dynamic> body) =>
      put('/api/User', body);
  Future<ApiEnvelope> deleteUser(int id) => delete('/api/User/$id');
  Future<ApiEnvelope> getUserInvitations(int id) =>
      get('/api/User/$id/invitations');
  Future<ApiEnvelope> getInvitedByList(int id) =>
      get('/api/User/$id/invited-by-list');
  Future<ApiEnvelope> getUserNotifications(int id) =>
      get('/api/User/$id/notification');
  Future<ApiEnvelope> deleteUserNotifications(int id) =>
      delete('/api/User/$id/notification');

  Future<ApiEnvelope> getMyMonasteries() => get('/api/v1/monasteries/mine');
  Future<ApiEnvelope> getMonastery(int id) => get('/api/v1/monasteries/$id');
  Future<ApiEnvelope> createMonastery(Map<String, dynamic> body) =>
      post('/api/v1/monasteries', body);
  Future<ApiEnvelope> updateMonastery(int id, Map<String, dynamic> body) =>
      put('/api/v1/monasteries/$id', body);
  Future<ApiEnvelope> inviteMember(int id, Map<String, dynamic> body) =>
      post('/api/v1/monasteries/$id/invitations', body);
  Future<ApiEnvelope> respondInvitation(int id, int status) =>
      post('/api/v1/monasteries/invitations/$id/respond', {'status': status});
  Future<ApiEnvelope> getMembers(int id) =>
      get('/api/v1/monasteries/$id/members');
  Future<ApiEnvelope> updateMemberRole(int id, int memberUserId, int role) =>
      put('/api/v1/monasteries/$id/members/$memberUserId/role', {'role': role});
  Future<ApiEnvelope> removeMember(int id, int memberUserId) =>
      delete('/api/v1/monasteries/$id/members/$memberUserId');

  Future<ApiEnvelope> getDonations(Map<String, dynamic> query) =>
      get('/api/v1/donations', query: query);
  Future<ApiEnvelope> getDonation(int id) => get('/api/v1/donations/$id');
  Future<ApiEnvelope> requestDonation(Map<String, dynamic> body) =>
      post('/api/v1/donations/request', body);
  Future<ApiEnvelope> createManualDonation(Map<String, dynamic> body) =>
      post('/api/v1/donations/manual', body);
  Future<ApiEnvelope> reviewDonation(int id, Map<String, dynamic> body) =>
      put('/api/v1/donations/$id/review', body);
  Future<ApiEnvelope> scheduleDonation(int id, Map<String, dynamic> body) =>
      put('/api/v1/donations/$id/schedule', body);
  Future<ApiEnvelope> completeDonation(int id) =>
      put('/api/v1/donations/$id/complete', const {});
  Future<ApiEnvelope> cancelDonation(int id) =>
      put('/api/v1/donations/$id/cancel', const {});

  Future<ApiEnvelope> createNotification(Map<String, dynamic> body) =>
      post('/api/v1/Notifications/create', body);
  Future<ApiEnvelope> readNotification(int id, int userId) => put(
    '/api/v1/Notifications/read-noti',
    {'notificationId': id, 'userId': userId},
  );
  Future<ApiEnvelope> deleteNotification(int id, int userId) => delete(
    '/api/v1/Notifications/delete',
    data: {'notificationId': id, 'userId': userId},
  );

  Future<ApiEnvelope> get(String path, {Map<String, dynamic>? query}) async {
    return _request(() => _dio.get<dynamic>(path, queryParameters: query));
  }

  Future<ApiEnvelope> post(String path, Map<String, dynamic> body) async {
    return _request(() => _dio.post<dynamic>(path, data: body));
  }

  Future<ApiEnvelope> put(String path, Map<String, dynamic> body) async {
    return _request(() => _dio.put<dynamic>(path, data: body));
  }

  Future<ApiEnvelope> delete(String path, {Map<String, dynamic>? data}) async {
    return _request(() => _dio.delete<dynamic>(path, data: data));
  }

  Future<ApiEnvelope> _request(Future<Response<dynamic>> Function() run) async {
    try {
      final response = await run();
      return ApiEnvelope.fromJson(response.data);
    } on DioException catch (error) {
      final data = error.response?.data;
      final envelope = ApiEnvelope.tryFromJson(data);
      throw ApiException(
        envelope?.message ??
            error.response?.statusMessage ??
            error.message ??
            'API request failed',
      );
    }
  }
}

class ApiEnvelope {
  const ApiEnvelope({
    required this.isSuccess,
    required this.message,
    this.data,
    this.listData,
  });

  final bool isSuccess;
  final String message;
  final dynamic data;
  final dynamic listData;

  Map<String, dynamic> get dataAsMap {
    final value = data;
    if (value is Map<String, dynamic>) return value;
    if (value is Map) return Map<String, dynamic>.from(value);
    return const {};
  }

  List<Map<String, dynamic>> get listAsMaps {
    final value = listData ?? data;
    if (value is List) {
      return value
          .whereType<Map>()
          .map((item) => Map<String, dynamic>.from(item))
          .toList();
    }
    return const [];
  }

  static ApiEnvelope? tryFromJson(dynamic json) {
    try {
      return ApiEnvelope.fromJson(json);
    } catch (_) {
      return null;
    }
  }

  factory ApiEnvelope.fromJson(dynamic json) {
    if (json is Map) {
      final map = Map<String, dynamic>.from(json);
      final hasEnvelope =
          map.containsKey('isSuccess') || map.containsKey('IsSuccess');
      final success = map['isSuccess'] ?? map['IsSuccess'];
      final message = map['message'] ?? map['Message'] ?? '';
      return ApiEnvelope(
        isSuccess: hasEnvelope ? success == true : true,
        message: message.toString(),
        data: map['data'] ?? map['Data'] ?? (hasEnvelope ? null : map),
        listData: map['listData'] ?? map['ListData'],
      );
    }

    if (json is List) {
      return ApiEnvelope(isSuccess: true, message: '', listData: json);
    }

    return ApiEnvelope(isSuccess: true, message: '', data: json);
  }
}

class ApiException implements Exception {
  ApiException(this.message);

  final String message;

  @override
  String toString() => message;
}
