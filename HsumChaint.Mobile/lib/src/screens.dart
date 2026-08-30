import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import 'api_client.dart';
import 'app_state.dart';

class AppShell extends ConsumerWidget {
  const AppShell({super.key, required this.child});

  final Widget child;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final text = AppText(ref.watch(localeControllerProvider).locale);
    final path = GoRouterState.of(context).matchedLocation;
    final routes = [
      _NavItem('/monasteries', text.get('monasteries'), Icons.temple_buddhist),
      _NavItem('/donations', text.get('donations'), Icons.volunteer_activism),
      _NavItem(
        '/notifications',
        text.get('notifications'),
        Icons.notifications,
      ),
      _NavItem('/users', text.get('users'), Icons.people),
      _NavItem('/profile', text.get('profile'), Icons.person),
    ];
    final selected = routes.indexWhere((route) => route.path == path);

    return Scaffold(
      appBar: AppBar(
        title: Text(text.get('appTitle')),
        actions: [
          TextButton.icon(
            onPressed: () => ref.read(localeControllerProvider).toggle(),
            icon: const Icon(Icons.translate),
            label: Text(text.get('language')),
          ),
          IconButton(
            tooltip: text.get('logout'),
            onPressed: () => ref.read(authControllerProvider).logout(),
            icon: const Icon(Icons.logout),
          ),
        ],
      ),
      body: SafeArea(child: child),
      bottomNavigationBar: NavigationBar(
        selectedIndex: selected < 0 ? 0 : selected,
        onDestinationSelected: (index) => context.go(routes[index].path),
        destinations: [
          for (final route in routes)
            NavigationDestination(icon: Icon(route.icon), label: route.label),
        ],
      ),
    );
  }
}

class AuthScreen extends ConsumerStatefulWidget {
  const AuthScreen({super.key});

  @override
  ConsumerState<AuthScreen> createState() => _AuthScreenState();
}

class _AuthScreenState extends ConsumerState<AuthScreen>
    with SingleTickerProviderStateMixin {
  late final TabController _tabController;
  final _loginPhone = TextEditingController();
  final _loginPassword = TextEditingController();
  final _name = TextEditingController();
  final _registerPhone = TextEditingController();
  final _registerPassword = TextEditingController();
  final _email = TextEditingController();
  final _contactPhone = TextEditingController();
  final _monasteryName = TextEditingController();
  final _monasteryAddress = TextEditingController();
  int _userType = 0;

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 2, vsync: this);
  }

  @override
  void dispose() {
    _tabController.dispose();
    for (final controller in [
      _loginPhone,
      _loginPassword,
      _name,
      _registerPhone,
      _registerPassword,
      _email,
      _contactPhone,
      _monasteryName,
      _monasteryAddress,
    ]) {
      controller.dispose();
    }
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final auth = ref.watch(authControllerProvider);
    final text = AppText(ref.watch(localeControllerProvider).locale);

    return Scaffold(
      appBar: AppBar(
        title: Text(text.get('appTitle')),
        actions: [
          TextButton.icon(
            onPressed: () => ref.read(localeControllerProvider).toggle(),
            icon: const Icon(Icons.translate),
            label: Text(text.get('language')),
          ),
        ],
        bottom: TabBar(
          controller: _tabController,
          tabs: [
            Tab(text: text.get('login')),
            Tab(text: text.get('register')),
          ],
        ),
      ),
      body: SafeArea(
        child: Center(
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 520),
            child: TabBarView(
              controller: _tabController,
              children: [
                _AuthPanel(
                  children: [
                    AppField(controller: _loginPhone, label: text.get('phone')),
                    AppField(
                      controller: _loginPassword,
                      label: text.get('password'),
                      obscureText: true,
                    ),
                    FilledButton.icon(
                      onPressed: auth.isBusy
                          ? null
                          : () async {
                              final ok = await ref
                                  .read(authControllerProvider)
                                  .login(_loginPhone.text, _loginPassword.text);
                              if (ok && context.mounted) {
                                context.go('/monasteries');
                              }
                            },
                      icon: const Icon(Icons.login),
                      label: Text(text.get('login')),
                    ),
                    StatusText(message: auth.message),
                  ],
                ),
                _AuthPanel(
                  children: [
                    AppField(controller: _name, label: text.get('name')),
                    AppField(
                      controller: _registerPhone,
                      label: text.get('phone'),
                    ),
                    AppField(
                      controller: _registerPassword,
                      label: text.get('password'),
                      obscureText: true,
                    ),
                    AppDropdown(
                      label: text.get('userType'),
                      value: _userType,
                      items: const [0, 1],
                      labelFor: (value) => text.enumLabel('UserType', value),
                      onChanged: (value) => setState(() => _userType = value),
                    ),
                    AppField(controller: _email, label: text.get('email')),
                    AppField(
                      controller: _contactPhone,
                      label: text.get('contactPhone'),
                    ),
                    if (_userType == 1) ...[
                      AppField(
                        controller: _monasteryName,
                        label: text.get('monasteryName'),
                      ),
                      AppField(
                        controller: _monasteryAddress,
                        label: text.get('monasteryAddress'),
                      ),
                    ],
                    FilledButton.icon(
                      onPressed: auth.isBusy
                          ? null
                          : () async {
                              final ok = await ref
                                  .read(authControllerProvider)
                                  .register({
                                    'name': _name.text,
                                    'phoneNumber': _registerPhone.text,
                                    'password': _registerPassword.text,
                                    'userType': _userType,
                                    'email': nullableText(_email.text),
                                    'contactPhoneNumber': nullableText(
                                      _contactPhone.text,
                                    ),
                                    'monasteryName': nullableText(
                                      _monasteryName.text,
                                    ),
                                    'monasteryAddress': nullableText(
                                      _monasteryAddress.text,
                                    ),
                                  });
                              if (ok && mounted) _tabController.index = 0;
                            },
                      icon: const Icon(Icons.person_add),
                      label: Text(text.get('register')),
                    ),
                    StatusText(message: auth.message),
                  ],
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class MonasteriesScreen extends ConsumerStatefulWidget {
  const MonasteriesScreen({super.key});

  @override
  ConsumerState<MonasteriesScreen> createState() => _MonasteriesScreenState();
}

class _MonasteriesScreenState extends ConsumerState<MonasteriesScreen> {
  late Future<ApiEnvelope> _future;

  @override
  void initState() {
    super.initState();
    _reload();
  }

  void _reload() {
    _future = ref.read(apiClientProvider).getMyMonasteries();
  }

  @override
  Widget build(BuildContext context) {
    final text = AppText(ref.watch(localeControllerProvider).locale);

    return ApiListScaffold(
      title: text.get('monasteries'),
      future: _future,
      onRefresh: () => setState(_reload),
      actions: [
        IconButton(
          tooltip: text.get('create'),
          onPressed: () => showMonasteryForm(context, ref, onDone: _refresh),
          icon: const Icon(Icons.add_business),
        ),
      ],
      itemBuilder: (item) {
        final id = intOf(item, 'id');
        return Card(
          child: ListTile(
            key: ValueKey('monastery-$id'),
            title: Text(display(item, 'monasteryName')),
            subtitle: Text(
              '${text.get('role')}: ${text.enumLabel('MonasteryRole', intOf(item, 'currentUserRole'))}\n${display(item, 'address')}',
            ),
            isThreeLine: true,
            trailing: Wrap(
              children: [
                IconButton(
                  tooltip: text.get('edit'),
                  onPressed: () => showMonasteryForm(
                    context,
                    ref,
                    item: item,
                    onDone: _refresh,
                  ),
                  icon: const Icon(Icons.edit),
                ),
                IconButton(
                  tooltip: text.get('members'),
                  onPressed: () => showMembersSheet(context, ref, id, _refresh),
                  icon: const Icon(Icons.groups),
                ),
              ],
            ),
          ),
        );
      },
    );
  }

  void _refresh() => setState(_reload);
}

class DonationsScreen extends ConsumerStatefulWidget {
  const DonationsScreen({super.key});

  @override
  ConsumerState<DonationsScreen> createState() => _DonationsScreenState();
}

class _DonationsScreenState extends ConsumerState<DonationsScreen> {
  final _monasteryId = TextEditingController();
  final _donorId = TextEditingController();
  int? _status;
  late Future<ApiEnvelope> _future;

  @override
  void initState() {
    super.initState();
    _reload();
  }

  @override
  void dispose() {
    _monasteryId.dispose();
    _donorId.dispose();
    super.dispose();
  }

  void _reload() {
    _future = ref.read(apiClientProvider).getDonations({
      if (int.tryParse(_monasteryId.text) != null)
        'monasterySpaceId': int.parse(_monasteryId.text),
      if (int.tryParse(_donorId.text) != null)
        'donorId': int.parse(_donorId.text),
      if (_status != null) 'status': _status,
    });
  }

  @override
  Widget build(BuildContext context) {
    final text = AppText(ref.watch(localeControllerProvider).locale);

    return ApiListScaffold(
      title: text.get('donations'),
      future: _future,
      onRefresh: () => setState(_reload),
      header: Column(
        children: [
          Row(
            children: [
              Expanded(
                child: AppField(
                  controller: _monasteryId,
                  label: 'Monastery ID',
                  keyboardType: TextInputType.number,
                ),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: AppField(
                  controller: _donorId,
                  label: 'Donor ID',
                  keyboardType: TextInputType.number,
                ),
              ),
            ],
          ),
          AppDropdown<int?>(
            label: text.get('status'),
            value: _status,
            items: const [null, 0, 1, 2, 3, 4, 5, 6, 7],
            labelFor: (value) =>
                value == null ? '-' : text.enumLabel('DonationStatus', value),
            onChanged: (value) => setState(() {
              _status = value;
              _reload();
            }),
          ),
          Row(
            children: [
              Expanded(
                child: FilledButton.icon(
                  onPressed: () => showDonationForm(
                    context,
                    ref,
                    manual: false,
                    onDone: _refresh,
                  ),
                  icon: const Icon(Icons.add),
                  label: Text(text.get('requestDonation')),
                ),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: FilledButton.tonalIcon(
                  onPressed: () => showDonationForm(
                    context,
                    ref,
                    manual: true,
                    onDone: _refresh,
                  ),
                  icon: const Icon(Icons.edit_note),
                  label: Text(text.get('manualDonation')),
                ),
              ),
            ],
          ),
        ],
      ),
      itemBuilder: (item) {
        final id = intOf(item, 'id');
        return Card(
          child: ExpansionTile(
            key: ValueKey('donation-$id'),
            title: Text(
              '${text.enumLabel('DonationType', intOf(item, 'donationType'))} #$id',
            ),
            subtitle: Text(
              '${text.get('status')}: ${text.enumLabel('DonationStatus', intOf(item, 'status'))}',
            ),
            childrenPadding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
            children: [
              DetailRows(item: item),
              Wrap(
                spacing: 8,
                children: [
                  OutlinedButton.icon(
                    onPressed: () => showDonationDetail(context, ref, id),
                    icon: const Icon(Icons.info),
                    label: Text(text.get('detail')),
                  ),
                  OutlinedButton.icon(
                    onPressed: () => showReviewForm(context, ref, id, _refresh),
                    icon: const Icon(Icons.fact_check),
                    label: Text(text.get('review')),
                  ),
                  OutlinedButton.icon(
                    onPressed: () =>
                        showScheduleForm(context, ref, id, _refresh),
                    icon: const Icon(Icons.schedule),
                    label: Text(text.get('schedule')),
                  ),
                  OutlinedButton.icon(
                    onPressed: () => runApiAction(
                      context,
                      () => ref.read(apiClientProvider).completeDonation(id),
                      _refresh,
                    ),
                    icon: const Icon(Icons.check_circle),
                    label: Text(text.get('complete')),
                  ),
                  OutlinedButton.icon(
                    onPressed: () => runApiAction(
                      context,
                      () => ref.read(apiClientProvider).cancelDonation(id),
                      _refresh,
                    ),
                    icon: const Icon(Icons.cancel),
                    label: Text(text.get('cancel')),
                  ),
                ],
              ),
            ],
          ),
        );
      },
    );
  }

  void _refresh() => setState(_reload);
}

class NotificationsScreen extends ConsumerStatefulWidget {
  const NotificationsScreen({super.key});

  @override
  ConsumerState<NotificationsScreen> createState() =>
      _NotificationsScreenState();
}

class _NotificationsScreenState extends ConsumerState<NotificationsScreen> {
  late Future<ApiEnvelope> _future;

  @override
  void initState() {
    super.initState();
    _reload();
  }

  void _reload() {
    final userId = ref.read(authControllerProvider).session.userId ?? 0;
    _future = ref.read(apiClientProvider).getUserNotifications(userId);
  }

  @override
  Widget build(BuildContext context) {
    final text = AppText(ref.watch(localeControllerProvider).locale);
    final userId = ref.watch(authControllerProvider).session.userId ?? 0;

    return ApiListScaffold(
      title: text.get('notifications'),
      future: _future,
      onRefresh: () => setState(_reload),
      actions: [
        IconButton(
          tooltip: text.get('create'),
          onPressed: () => showNotificationForm(context, ref, onDone: _refresh),
          icon: const Icon(Icons.add_alert),
        ),
        IconButton(
          tooltip: text.get('delete'),
          onPressed: () => runApiAction(
            context,
            () => ref.read(apiClientProvider).deleteUserNotifications(userId),
            _refresh,
          ),
          icon: const Icon(Icons.delete_sweep),
        ),
      ],
      itemBuilder: (item) {
        final id = intOf(item, 'id');
        return Card(
          child: ListTile(
            key: ValueKey('notification-$id'),
            leading: Icon(
              boolOf(item, 'isRead')
                  ? Icons.mark_email_read
                  : Icons.mark_email_unread,
            ),
            title: Text(display(item, 'message')),
            subtitle: Text(
              '${display(item, 'type')} • ${display(item, 'createdAt')}',
            ),
            trailing: Wrap(
              children: [
                IconButton(
                  tooltip: text.get('markRead'),
                  onPressed: () => runApiAction(
                    context,
                    () => ref
                        .read(apiClientProvider)
                        .readNotification(id, userId),
                    _refresh,
                  ),
                  icon: const Icon(Icons.done),
                ),
                IconButton(
                  tooltip: text.get('delete'),
                  onPressed: () => runApiAction(
                    context,
                    () => ref
                        .read(apiClientProvider)
                        .deleteNotification(id, userId),
                    _refresh,
                  ),
                  icon: const Icon(Icons.delete),
                ),
              ],
            ),
          ),
        );
      },
    );
  }

  void _refresh() => setState(_reload);
}

class UsersScreen extends ConsumerStatefulWidget {
  const UsersScreen({super.key});

  @override
  ConsumerState<UsersScreen> createState() => _UsersScreenState();
}

class _UsersScreenState extends ConsumerState<UsersScreen> {
  late Future<ApiEnvelope> _future;

  @override
  void initState() {
    super.initState();
    _reload();
  }

  void _reload() {
    _future = ref.read(apiClientProvider).getUsers();
  }

  @override
  Widget build(BuildContext context) {
    final text = AppText(ref.watch(localeControllerProvider).locale);

    return ApiListScaffold(
      title: text.get('users'),
      future: _future,
      onRefresh: () => setState(_reload),
      itemBuilder: (item) {
        final id = intOf(item, 'id');
        return Card(
          child: ExpansionTile(
            key: ValueKey('user-$id'),
            title: Text(display(item, 'name')),
            subtitle: Text(display(item, 'phoneNumber')),
            childrenPadding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
            children: [
              DetailRows(item: item),
              Wrap(
                spacing: 8,
                children: [
                  OutlinedButton.icon(
                    onPressed: () => showUserDetail(context, ref, id),
                    icon: const Icon(Icons.info),
                    label: Text(text.get('detail')),
                  ),
                  OutlinedButton.icon(
                    onPressed: () =>
                        showUserForm(context, ref, item, onDone: _refresh),
                    icon: const Icon(Icons.edit),
                    label: Text(text.get('edit')),
                  ),
                  OutlinedButton.icon(
                    onPressed: () => showInvitations(context, ref, id),
                    icon: const Icon(Icons.mail),
                    label: Text(text.get('invite')),
                  ),
                  OutlinedButton.icon(
                    onPressed: () => runApiAction(
                      context,
                      () => ref.read(apiClientProvider).deleteUser(id),
                      _refresh,
                    ),
                    icon: const Icon(Icons.delete),
                    label: Text(text.get('delete')),
                  ),
                ],
              ),
            ],
          ),
        );
      },
    );
  }

  void _refresh() => setState(_reload);
}

class ProfileScreen extends ConsumerWidget {
  const ProfileScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final text = AppText(ref.watch(localeControllerProvider).locale);
    final auth = ref.watch(authControllerProvider);
    final userId = auth.session.userId ?? 0;

    return FutureBuilder<ApiEnvelope>(
      future: ref.watch(apiClientProvider).getUser(userId),
      builder: (context, snapshot) {
        final item = snapshot.data?.dataAsMap ?? const <String, dynamic>{};
        return PageFrame(
          title: text.get('profile'),
          actions: [
            IconButton(
              tooltip: text.get('logout'),
              onPressed: () => ref.read(authControllerProvider).logout(),
              icon: const Icon(Icons.logout),
            ),
          ],
          child: ListView(
            padding: const EdgeInsets.all(16),
            children: [
              InfoTile(label: text.get('apiUrl'), value: apiUrl),
              InfoTile(label: 'User ID', value: userId.toString()),
              InfoTile(
                label: text.get('userType'),
                value: text.enumLabel('UserType', auth.session.userType ?? 0),
              ),
              if (snapshot.connectionState == ConnectionState.waiting)
                const LinearProgressIndicator(),
              if (item.isNotEmpty) DetailRows(item: item),
            ],
          ),
        );
      },
    );
  }
}

class ApiListScaffold extends StatelessWidget {
  const ApiListScaffold({
    super.key,
    required this.title,
    required this.future,
    required this.onRefresh,
    required this.itemBuilder,
    this.actions = const [],
    this.header,
  });

  final String title;
  final Future<ApiEnvelope> future;
  final VoidCallback onRefresh;
  final Widget Function(Map<String, dynamic> item) itemBuilder;
  final List<Widget> actions;
  final Widget? header;

  @override
  Widget build(BuildContext context) {
    return PageFrame(
      title: title,
      actions: [
        IconButton(
          tooltip: MaterialLocalizations.of(
            context,
          ).refreshIndicatorSemanticLabel,
          onPressed: onRefresh,
          icon: const Icon(Icons.refresh),
        ),
        ...actions,
      ],
      child: FutureBuilder<ApiEnvelope>(
        future: future,
        builder: (context, snapshot) {
          final items =
              snapshot.data?.listAsMaps ?? const <Map<String, dynamic>>[];
          return RefreshIndicator(
            onRefresh: () async => onRefresh(),
            child: ListView(
              padding: const EdgeInsets.all(16),
              children: [
                if (header != null) ...[header!, const SizedBox(height: 12)],
                if (snapshot.connectionState == ConnectionState.waiting)
                  const LinearProgressIndicator(),
                if (snapshot.hasError)
                  ErrorBanner(message: snapshot.error.toString()),
                if (items.isEmpty &&
                    !snapshot.hasError &&
                    snapshot.connectionState != ConnectionState.waiting)
                  const EmptyState(),
                for (final item in items) itemBuilder(item),
              ],
            ),
          );
        },
      ),
    );
  }
}

class PageFrame extends StatelessWidget {
  const PageFrame({
    super.key,
    required this.title,
    required this.child,
    this.actions = const [],
  });

  final String title;
  final Widget child;
  final List<Widget> actions;

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Material(
          color: Theme.of(context).colorScheme.surface,
          child: ListTile(
            title: Text(title, style: Theme.of(context).textTheme.titleLarge),
            trailing: Wrap(spacing: 4, children: actions),
          ),
        ),
        Expanded(child: child),
      ],
    );
  }
}

class AppField extends StatelessWidget {
  const AppField({
    super.key,
    required this.controller,
    required this.label,
    this.keyboardType,
    this.obscureText = false,
    this.maxLines = 1,
  });

  final TextEditingController controller;
  final String label;
  final TextInputType? keyboardType;
  final bool obscureText;
  final int maxLines;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 10),
      child: TextField(
        controller: controller,
        keyboardType: keyboardType,
        obscureText: obscureText,
        maxLines: maxLines,
        inputFormatters: keyboardType == TextInputType.number
            ? [FilteringTextInputFormatter.digitsOnly]
            : null,
        decoration: InputDecoration(labelText: label),
      ),
    );
  }
}

class AppDropdown<T> extends StatelessWidget {
  const AppDropdown({
    super.key,
    required this.label,
    required this.value,
    required this.items,
    required this.labelFor,
    required this.onChanged,
  });

  final String label;
  final T value;
  final List<T> items;
  final String Function(T value) labelFor;
  final ValueChanged<T> onChanged;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 10),
      child: DropdownButtonFormField<T>(
        initialValue: value,
        decoration: InputDecoration(labelText: label),
        items: [
          for (final item in items)
            DropdownMenuItem<T>(value: item, child: Text(labelFor(item))),
        ],
        onChanged: (value) {
          if (value != null || items.contains(null)) onChanged(value as T);
        },
      ),
    );
  }
}

class DetailRows extends StatelessWidget {
  const DetailRows({super.key, required this.item});

  final Map<String, dynamic> item;

  @override
  Widget build(BuildContext context) {
    final entries = item.entries
        .where(
          (entry) => entry.value != null && entry.value.toString().isNotEmpty,
        )
        .toList();
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        for (final entry in entries)
          Padding(
            padding: const EdgeInsets.only(bottom: 6),
            child: Text('${entry.key}: ${entry.value}'),
          ),
      ],
    );
  }
}

class InfoTile extends StatelessWidget {
  const InfoTile({super.key, required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: ListTile(title: Text(label), subtitle: SelectableText(value)),
    );
  }
}

class StatusText extends StatelessWidget {
  const StatusText({super.key, this.message});

  final String? message;

  @override
  Widget build(BuildContext context) {
    if (message == null || message!.isEmpty) return const SizedBox.shrink();
    return Padding(
      padding: const EdgeInsets.only(top: 12),
      child: Text(message!, textAlign: TextAlign.center),
    );
  }
}

class EmptyState extends ConsumerWidget {
  const EmptyState({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final text = AppText(ref.watch(localeControllerProvider).locale);
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(40),
        child: Text(text.get('empty')),
      ),
    );
  }
}

class ErrorBanner extends StatelessWidget {
  const ErrorBanner({super.key, required this.message});

  final String message;

  @override
  Widget build(BuildContext context) {
    return Card(
      color: Theme.of(context).colorScheme.errorContainer,
      child: Padding(padding: const EdgeInsets.all(12), child: Text(message)),
    );
  }
}

class _AuthPanel extends StatelessWidget {
  const _AuthPanel({required this.children});

  final List<Widget> children;

  @override
  Widget build(BuildContext context) {
    return ListView(padding: const EdgeInsets.all(16), children: children);
  }
}

class _NavItem {
  const _NavItem(this.path, this.label, this.icon);

  final String path;
  final String label;
  final IconData icon;
}

Future<void> showMonasteryForm(
  BuildContext context,
  WidgetRef ref, {
  Map<String, dynamic>? item,
  required VoidCallback onDone,
}) async {
  final text = AppText(ref.read(localeControllerProvider).locale);
  final name = TextEditingController(text: stringOf(item, 'monasteryName'));
  final description = TextEditingController(
    text: stringOf(item, 'description'),
  );
  final address = TextEditingController(text: stringOf(item, 'address'));
  await showAppDialog(
    context,
    title: item == null ? text.get('create') : text.get('edit'),
    children: [
      AppField(controller: name, label: text.get('monasteryName')),
      AppField(controller: description, label: text.get('description')),
      AppField(controller: address, label: text.get('address')),
    ],
    onSubmit: () => item == null
        ? ref.read(apiClientProvider).createMonastery({
            'monasteryName': name.text,
            'description': nullableText(description.text),
            'address': nullableText(address.text),
          })
        : ref.read(apiClientProvider).updateMonastery(intOf(item, 'id'), {
            'monasteryName': name.text,
            'description': nullableText(description.text),
            'address': nullableText(address.text),
          }),
    onDone: onDone,
  );
  name.dispose();
  description.dispose();
  address.dispose();
}

Future<void> showMembersSheet(
  BuildContext context,
  WidgetRef ref,
  int monasteryId,
  VoidCallback onDone,
) async {
  final text = AppText(ref.read(localeControllerProvider).locale);
  final api = ref.read(apiClientProvider);
  await showModalBottomSheet<void>(
    context: context,
    showDragHandle: true,
    isScrollControlled: true,
    builder: (context) {
      return DraggableScrollableSheet(
        expand: false,
        initialChildSize: 0.75,
        builder: (context, controller) {
          return FutureBuilder<ApiEnvelope>(
            future: api.getMembers(monasteryId),
            builder: (context, snapshot) {
              final members =
                  snapshot.data?.listAsMaps ?? const <Map<String, dynamic>>[];
              return ListView(
                controller: controller,
                padding: const EdgeInsets.all(16),
                children: [
                  Row(
                    children: [
                      Expanded(
                        child: Text(
                          text.get('members'),
                          style: Theme.of(context).textTheme.titleLarge,
                        ),
                      ),
                      IconButton(
                        tooltip: text.get('invite'),
                        onPressed: () =>
                            showInviteForm(context, ref, monasteryId, onDone),
                        icon: const Icon(Icons.person_add),
                      ),
                    ],
                  ),
                  if (snapshot.connectionState == ConnectionState.waiting)
                    const LinearProgressIndicator(),
                  for (final member in members)
                    Card(
                      child: ListTile(
                        title: Text(display(member, 'userName')),
                        subtitle: Text(
                          '${display(member, 'phoneNumber')} • ${text.enumLabel('MonasteryRole', intOf(member, 'role'))}',
                        ),
                        trailing: Wrap(
                          children: [
                            IconButton(
                              tooltip: text.get('role'),
                              onPressed: () => showRoleForm(
                                context,
                                ref,
                                monasteryId,
                                intOf(member, 'userId'),
                                intOf(member, 'role'),
                                onDone,
                              ),
                              icon: const Icon(Icons.admin_panel_settings),
                            ),
                            IconButton(
                              tooltip: text.get('delete'),
                              onPressed: () => runApiAction(
                                context,
                                () => api.removeMember(
                                  monasteryId,
                                  intOf(member, 'userId'),
                                ),
                                onDone,
                              ),
                              icon: const Icon(Icons.person_remove),
                            ),
                          ],
                        ),
                      ),
                    ),
                ],
              );
            },
          );
        },
      );
    },
  );
}

Future<void> showInviteForm(
  BuildContext context,
  WidgetRef ref,
  int monasteryId,
  VoidCallback onDone,
) async {
  final text = AppText(ref.read(localeControllerProvider).locale);
  final userId = TextEditingController();
  final phone = TextEditingController();
  var role = 3;
  await showAppDialog(
    context,
    title: text.get('invite'),
    children: [
      AppField(
        controller: userId,
        label: 'User ID',
        keyboardType: TextInputType.number,
      ),
      AppField(controller: phone, label: text.get('phone')),
      StatefulBuilder(
        builder: (context, setLocalState) {
          return AppDropdown(
            label: text.get('role'),
            value: role,
            items: const [1, 2, 3],
            labelFor: (value) => text.enumLabel('MonasteryRole', value),
            onChanged: (value) => setLocalState(() => role = value),
          );
        },
      ),
    ],
    onSubmit: () => ref.read(apiClientProvider).inviteMember(monasteryId, {
      if (int.tryParse(userId.text) != null) 'userId': int.parse(userId.text),
      'phoneNumber': nullableText(phone.text),
      'role': role,
    }),
    onDone: onDone,
  );
  userId.dispose();
  phone.dispose();
}

Future<void> showRoleForm(
  BuildContext context,
  WidgetRef ref,
  int monasteryId,
  int memberUserId,
  int initialRole,
  VoidCallback onDone,
) async {
  final text = AppText(ref.read(localeControllerProvider).locale);
  var role = initialRole == 0 ? 3 : initialRole;
  await showAppDialog(
    context,
    title: text.get('role'),
    children: [
      StatefulBuilder(
        builder: (context, setLocalState) {
          return AppDropdown(
            label: text.get('role'),
            value: role,
            items: const [1, 2, 3],
            labelFor: (value) => text.enumLabel('MonasteryRole', value),
            onChanged: (value) => setLocalState(() => role = value),
          );
        },
      ),
    ],
    onSubmit: () => ref
        .read(apiClientProvider)
        .updateMemberRole(monasteryId, memberUserId, role),
    onDone: onDone,
  );
}

Future<void> showDonationForm(
  BuildContext context,
  WidgetRef ref, {
  required bool manual,
  required VoidCallback onDone,
}) async {
  final text = AppText(ref.read(localeControllerProvider).locale);
  final monasteryId = TextEditingController();
  final customType = TextEditingController();
  final amount = TextEditingController();
  final quantity = TextEditingController();
  final note = TextEditingController();
  final pickup = TextEditingController();
  final dropoff = TextEditingController();
  final donorId = TextEditingController();
  final donorName = TextEditingController();
  var type = 0;
  await showAppDialog(
    context,
    title: manual ? text.get('manualDonation') : text.get('requestDonation'),
    children: [
      AppField(
        controller: monasteryId,
        label: 'Monastery ID',
        keyboardType: TextInputType.number,
      ),
      StatefulBuilder(
        builder: (context, setLocalState) {
          return AppDropdown(
            label: text.get('donationType'),
            value: type,
            items: const [0, 1, 2, 3, 4, 5],
            labelFor: (value) => text.enumLabel('DonationType', value),
            onChanged: (value) => setLocalState(() => type = value),
          );
        },
      ),
      AppField(controller: customType, label: text.get('customType')),
      AppField(
        controller: amount,
        label: text.get('amount'),
        keyboardType: TextInputType.number,
      ),
      AppField(
        controller: quantity,
        label: text.get('quantity'),
        keyboardType: TextInputType.number,
      ),
      AppField(controller: pickup, label: '${text.get('pickupTime')} ISO'),
      AppField(controller: dropoff, label: '${text.get('dropoffTime')} ISO'),
      AppField(controller: note, label: text.get('note'), maxLines: 3),
      if (manual) ...[
        AppField(
          controller: donorId,
          label: 'Donor ID',
          keyboardType: TextInputType.number,
        ),
        AppField(controller: donorName, label: text.get('name')),
      ],
    ],
    onSubmit: () {
      final body = {
        'monasterySpaceId': int.tryParse(monasteryId.text) ?? 0,
        'donationType': type,
        'customDonationType': nullableText(customType.text),
        'amount': decimalOrNull(amount.text),
        'quantity': decimalOrNull(quantity.text),
        'pickupTime': nullableText(pickup.text),
        'dropoffTime': nullableText(dropoff.text),
        'note': nullableText(note.text),
        if (manual && int.tryParse(donorId.text) != null)
          'donorId': int.parse(donorId.text),
        if (manual) 'donorName': nullableText(donorName.text),
      };
      return manual
          ? ref.read(apiClientProvider).createManualDonation(body)
          : ref.read(apiClientProvider).requestDonation(body);
    },
    onDone: onDone,
  );
  for (final controller in [
    monasteryId,
    customType,
    amount,
    quantity,
    note,
    pickup,
    dropoff,
    donorId,
    donorName,
  ]) {
    controller.dispose();
  }
}

Future<void> showDonationDetail(
  BuildContext context,
  WidgetRef ref,
  int donationId,
) async {
  await showResultSheet(
    context,
    ref.read(apiClientProvider).getDonation(donationId),
  );
}

Future<void> showReviewForm(
  BuildContext context,
  WidgetRef ref,
  int donationId,
  VoidCallback onDone,
) async {
  final text = AppText(ref.read(localeControllerProvider).locale);
  final note = TextEditingController();
  var status = 3;
  await showAppDialog(
    context,
    title: text.get('review'),
    children: [
      StatefulBuilder(
        builder: (context, setLocalState) {
          return AppDropdown(
            label: text.get('status'),
            value: status,
            items: const [3, 6],
            labelFor: (value) => text.enumLabel('DonationStatus', value),
            onChanged: (value) => setLocalState(() => status = value),
          );
        },
      ),
      AppField(controller: note, label: text.get('note'), maxLines: 3),
    ],
    onSubmit: () => ref.read(apiClientProvider).reviewDonation(donationId, {
      'status': status,
      'note': nullableText(note.text),
    }),
    onDone: onDone,
  );
  note.dispose();
}

Future<void> showScheduleForm(
  BuildContext context,
  WidgetRef ref,
  int donationId,
  VoidCallback onDone,
) async {
  final text = AppText(ref.read(localeControllerProvider).locale);
  final pickup = TextEditingController();
  final dropoff = TextEditingController();
  await showAppDialog(
    context,
    title: text.get('schedule'),
    children: [
      AppField(controller: pickup, label: '${text.get('pickupTime')} ISO'),
      AppField(controller: dropoff, label: '${text.get('dropoffTime')} ISO'),
    ],
    onSubmit: () => ref.read(apiClientProvider).scheduleDonation(donationId, {
      'pickupTime': nullableText(pickup.text),
      'dropoffTime': nullableText(dropoff.text),
    }),
    onDone: onDone,
  );
  pickup.dispose();
  dropoff.dispose();
}

Future<void> showNotificationForm(
  BuildContext context,
  WidgetRef ref, {
  required VoidCallback onDone,
}) async {
  final text = AppText(ref.read(localeControllerProvider).locale);
  final userId = TextEditingController(
    text: ref.read(authControllerProvider).session.userId?.toString() ?? '',
  );
  final type = TextEditingController(text: 'System');
  final message = TextEditingController();
  await showAppDialog(
    context,
    title: text.get('notifications'),
    children: [
      AppField(
        controller: userId,
        label: 'User ID',
        keyboardType: TextInputType.number,
      ),
      AppField(controller: type, label: text.get('donationType')),
      AppField(controller: message, label: text.get('message'), maxLines: 3),
    ],
    onSubmit: () => ref.read(apiClientProvider).createNotification({
      'userId': int.tryParse(userId.text) ?? 0,
      'notificationType': type.text,
      'message': message.text,
    }),
    onDone: onDone,
  );
  userId.dispose();
  type.dispose();
  message.dispose();
}

Future<void> showUserDetail(BuildContext context, WidgetRef ref, int id) async {
  await showResultSheet(context, ref.read(apiClientProvider).getUser(id));
}

Future<void> showInvitations(
  BuildContext context,
  WidgetRef ref,
  int id,
) async {
  final text = AppText(ref.read(localeControllerProvider).locale);
  final api = ref.read(apiClientProvider);
  await showModalBottomSheet<void>(
    context: context,
    showDragHandle: true,
    isScrollControlled: true,
    builder: (context) {
      return DraggableScrollableSheet(
        expand: false,
        initialChildSize: 0.75,
        builder: (context, controller) {
          return FutureBuilder<List<ApiEnvelope>>(
            future: Future.wait([
              api.getUserInvitations(id),
              api.getInvitedByList(id),
            ]),
            builder: (context, snapshot) {
              final invited = snapshot.data?.first.listAsMaps ?? const [];
              final invitedBy = snapshot.data?.last.listAsMaps ?? const [];
              final rows = [...invited, ...invitedBy];
              return ListView(
                controller: controller,
                padding: const EdgeInsets.all(16),
                children: [
                  Text(
                    text.get('invite'),
                    style: Theme.of(context).textTheme.titleLarge,
                  ),
                  if (snapshot.connectionState == ConnectionState.waiting)
                    const LinearProgressIndicator(),
                  for (final invite in rows)
                    Card(
                      child: ListTile(
                        title: Text('#${intOf(invite, 'id')}'),
                        subtitle: Text(
                          '${text.get('role')}: ${text.enumLabel('MonasteryRole', intOf(invite, 'role'))}\n${text.get('status')}: ${text.enumLabel('InvitationStatus', intOf(invite, 'status'))}',
                        ),
                        trailing: Wrap(
                          children: [
                            IconButton(
                              tooltip: text.get('accept'),
                              onPressed: () => runApiAction(
                                context,
                                () => api.respondInvitation(
                                  intOf(invite, 'id'),
                                  1,
                                ),
                                () {},
                              ),
                              icon: const Icon(Icons.check),
                            ),
                            IconButton(
                              tooltip: text.get('reject'),
                              onPressed: () => runApiAction(
                                context,
                                () => api.respondInvitation(
                                  intOf(invite, 'id'),
                                  2,
                                ),
                                () {},
                              ),
                              icon: const Icon(Icons.close),
                            ),
                          ],
                        ),
                      ),
                    ),
                ],
              );
            },
          );
        },
      );
    },
  );
}

Future<void> showUserForm(
  BuildContext context,
  WidgetRef ref,
  Map<String, dynamic> item, {
  required VoidCallback onDone,
}) async {
  final text = AppText(ref.read(localeControllerProvider).locale);
  final name = TextEditingController(text: stringOf(item, 'name'));
  final phone = TextEditingController(text: stringOf(item, 'phoneNumber'));
  final email = TextEditingController(text: stringOf(item, 'email'));
  final contactPhone = TextEditingController(
    text: stringOf(item, 'contactPhoneNumber'),
  );
  var userType = intOf(item, 'userType');
  await showAppDialog(
    context,
    title: text.get('edit'),
    children: [
      AppField(controller: name, label: text.get('name')),
      AppField(controller: phone, label: text.get('phone')),
      StatefulBuilder(
        builder: (context, setLocalState) {
          return AppDropdown(
            label: text.get('userType'),
            value: userType,
            items: const [0, 1],
            labelFor: (value) => text.enumLabel('UserType', value),
            onChanged: (value) => setLocalState(() => userType = value),
          );
        },
      ),
      AppField(controller: email, label: text.get('email')),
      AppField(controller: contactPhone, label: text.get('contactPhone')),
    ],
    onSubmit: () => ref.read(apiClientProvider).updateUser({
      'id': intOf(item, 'id'),
      'name': name.text,
      'phoneNumber': phone.text,
      'userType': userType,
      'email': nullableText(email.text),
      'contactPhoneNumber': nullableText(contactPhone.text),
    }),
    onDone: onDone,
  );
  name.dispose();
  phone.dispose();
  email.dispose();
  contactPhone.dispose();
}

Future<void> showResultSheet(
  BuildContext context,
  Future<ApiEnvelope> future,
) async {
  await showModalBottomSheet<void>(
    context: context,
    showDragHandle: true,
    isScrollControlled: true,
    builder: (context) {
      return FutureBuilder<ApiEnvelope>(
        future: future,
        builder: (context, snapshot) {
          final item = snapshot.data?.dataAsMap ?? const <String, dynamic>{};
          return ListView(
            padding: const EdgeInsets.all(16),
            shrinkWrap: true,
            children: [
              if (snapshot.connectionState == ConnectionState.waiting)
                const LinearProgressIndicator(),
              if (snapshot.hasError)
                ErrorBanner(message: snapshot.error.toString()),
              if (item.isNotEmpty) DetailRows(item: item),
            ],
          );
        },
      );
    },
  );
}

Future<void> showAppDialog(
  BuildContext context, {
  required String title,
  required List<Widget> children,
  required Future<ApiEnvelope> Function() onSubmit,
  required VoidCallback onDone,
}) async {
  var busy = false;
  await showDialog<void>(
    context: context,
    builder: (context) {
      return StatefulBuilder(
        builder: (context, setLocalState) {
          return AlertDialog(
            title: Text(title),
            content: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 520),
              child: SingleChildScrollView(child: Column(children: children)),
            ),
            actions: [
              TextButton(
                onPressed: busy ? null : () => Navigator.of(context).pop(),
                child: const Text('Cancel'),
              ),
              FilledButton(
                onPressed: busy
                    ? null
                    : () async {
                        setLocalState(() => busy = true);
                        await runApiAction(context, onSubmit, onDone);
                        if (context.mounted) Navigator.of(context).pop();
                      },
                child: busy
                    ? const SizedBox.square(
                        dimension: 18,
                        child: CircularProgressIndicator(strokeWidth: 2),
                      )
                    : const Text('Save'),
              ),
            ],
          );
        },
      );
    },
  );
}

Future<void> runApiAction(
  BuildContext context,
  Future<ApiEnvelope> Function() action,
  VoidCallback onDone,
) async {
  try {
    final response = await action();
    onDone();
    if (context.mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(response.message.isEmpty ? 'Done' : response.message),
        ),
      );
    }
  } on ApiException catch (error) {
    if (context.mounted) {
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text(error.message)));
    }
  }
}

String display(Map<String, dynamic> item, String key) {
  final value = valueOf(item, key);
  if (value == null || value.toString().isEmpty) return '-';
  return value.toString();
}

String stringOf(Map<String, dynamic>? item, String key) {
  if (item == null) return '';
  final value = valueOf(item, key);
  return value?.toString() ?? '';
}

int intOf(Map<String, dynamic> item, String key) {
  final value = valueOf(item, key);
  if (value is int) return value;
  if (value is num) return value.toInt();
  return int.tryParse(value?.toString() ?? '') ?? 0;
}

bool boolOf(Map<String, dynamic> item, String key) {
  final value = valueOf(item, key);
  if (value is bool) return value;
  return value?.toString().toLowerCase() == 'true';
}

Object? valueOf(Map<String, dynamic> item, String key) {
  return item[key] ?? item[key[0].toUpperCase() + key.substring(1)];
}

String? nullableText(String value) {
  final trimmed = value.trim();
  return trimmed.isEmpty ? null : trimmed;
}

num? decimalOrNull(String value) {
  return num.tryParse(value.trim());
}
