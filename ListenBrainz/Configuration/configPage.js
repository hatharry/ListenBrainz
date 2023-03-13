define(['baseView', 'loading', 'emby-input', 'emby-button', 'emby-checkbox', 'emby-scroller', 'emby-select'], function (BaseView, loading) {
    'use strict';

    function onSubmit(e) {

        e.preventDefault();

        var instance = this;
        var form = this.view;

        var username = form.querySelector('.username').value;
        var password = form.querySelector('.password').value;

        //Load the config again in case another user has updated their ListenBrainz info
        instance.loadConfiguration().then(function () {
            instance.save(username, password);
        });

        // Disable default form submission
        return false;
    }

    function View(view, params) {
        BaseView.apply(this, arguments);

        this.configDefaults = {
            Username: '',
            SessionKey: '',
            MediaBrowserUserId: '',
            Options: {
                Scrobble: false
            }
        };

        view.querySelector('form').addEventListener('submit', onSubmit.bind(this));
        view.querySelector('.user').addEventListener('change', this.onUserChange.bind(this));
    }

    Object.assign(View.prototype, BaseView.prototype);

    View.prototype.populateInputs = function (userData) {

        var data = Object.assign(Object.assign({}, this.configDefaults), userData || {});

        this.view.querySelector('.username').value = data.Username;
        this.view.querySelector('.password').value = data.SessionKey;
        this.view.querySelector('.optionScrobble').checked = data.Options.Scrobble;
    };

    View.prototype.loadUsers = function () {

        var instance = this;

        return ApiClient.getUsers().then(function (users) {

            instance._users = users;
        });
    };

    View.prototype.loadConfiguration = function () {

        var instance = this;

        return ApiClient.getPluginConfiguration("FC7FB5C1-3DAF-4E74-988E-1FF4A66D2651").then(function (config) {

            instance._config = config;
        });
    };

    View.prototype.getCurrentSelectedUserId = function () {

        //Get the current user
        return this.view.querySelector('.user').value;
    };

    View.prototype.getCurrentSelectedUser = function () {

        //Get the current user
        var currentUserId = this.getCurrentSelectedUserId();

        var currentUser = this._config.ListenBrainzUsers.filter(function (user) {
            return user.MediaBrowserUserId === currentUserId;
        })[0];

        return currentUser;
    };

    View.prototype.getSelectedMBUser = function () {

        //Get the current user
        var currentUserId = this.getCurrentSelectedUserId();

        var currentUser = this._users.filter(function (user) {
            return user.Id === currentUserId;
        })[0];

        return currentUser;
    };

    View.prototype.onUserChange = function () {

        var currentUser = this.getCurrentSelectedUser();

        this.populateInputs(currentUser);
    };

    View.prototype.save = function (username, password) {

        var userConfig = this.getCurrentSelectedUser();

        //If the conig for the user doesnt exist, create one
        if (!userConfig) {
            userConfig = Object.assign(Object.assign({}, this.configDefaults), { MediaBrowserUserId: this.getCurrentSelectedUserId() });

            this._config.ListenBrainzUsers.push(userConfig);
        }

        userConfig.Username = username;
        userConfig.SessionKey = password;
        userConfig.Options = this.getUIOptionsValues();

        //Save
        this.doSave();

        return;
    };

    View.prototype.getUIOptionsValues = function () {

        var options = Object.assign({}, this.configDefaults.Options);

        options.Scrobble = this.view.querySelector('.optionScrobble').checked;

        return options;
    };

    View.prototype.doSave = function () {

        var instance = this;

        return ApiClient.updatePluginConfiguration("FC7FB5C1-3DAF-4E74-988E-1FF4A66D2651", this._config).then(function () {

            instance.onUserChange();
        });
    };

    View.prototype.onResume = function (options) {

        BaseView.prototype.onResume.apply(this, arguments);

        var instance = this;

        instance.loadUsers().then(function () {

            var html = instance._users.map(function (user) {
                return '<option value="' + user.Id + '">' + user.Name + '</option>';
            }).join('');

            var selectUser = instance.view.querySelector('.user');
            selectUser.innerHTML = html;

            instance.loadConfiguration().then(function () {
                instance.onUserChange();
            });
        });
    };

    return View;
});
