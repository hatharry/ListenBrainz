define(['baseView', 'loading', 'emby-input', 'emby-button', 'emby-checkbox', 'emby-scroller', 'emby-select'], function (BaseView, loading) {
    'use strict';

    function onSubmit(ev) {
        ev.preventDefault();
        loading.show();

        var instance = this;

        var form = ev.currentTarget;

        var currentUserId = instance.params.userId;

        fetchExistingConfiguration(currentUserId).then(function (currentUserConfig) {

            currentUserConfig.Username = form.querySelector('.username').value;
            currentUserConfig.SessionKey = form.querySelector('.password').value;
            currentUserConfig.Scrobble = form.querySelector('.optionScrobble').checked;

            ApiClient.updateTypedUserSettings(currentUserId, 'listenbrainz', currentUserConfig).then(
                function (result) {
                    Dashboard.processPluginConfigurationUpdateResult(result);
                    loadConfiguration(currentUserId, form);
                }
            );
        });

        return false;
    }

    function View(view, params) {
        BaseView.apply(this, arguments);

        view.querySelector('form').addEventListener('submit', onSubmit.bind(this));
    }

    Object.assign(View.prototype, BaseView.prototype);

    function fetchExistingConfiguration(userId) {

        return ApiClient.getTypedUserSettings(userId, 'listenbrainz');
    }

    function loadConfiguration(userId, form) {

        fetchExistingConfiguration(userId).then(function (currentUserConfig) {

            form.querySelector('.username').value = currentUserConfig.Username || '';
            form.querySelector('.password').value = currentUserConfig.SessionKey || '';
            form.querySelector('.optionScrobble').checked = currentUserConfig.Scrobble;

            loading.hide();
        });
    }

    View.prototype.onResume = function (options) {

        BaseView.prototype.onResume.apply(this, arguments);

        if (options.refresh) {
            loading.show();

            var view = this.view;
            var form = view.querySelector("form");
            var instance = this;

            loadConfiguration(instance.params.userId, form);
        }
    };

    return View;
});
