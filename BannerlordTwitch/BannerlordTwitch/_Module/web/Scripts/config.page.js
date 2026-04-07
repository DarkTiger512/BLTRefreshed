(function () {
    'use strict';

    var configHub = null;

    function createApp() {
        var app = new Vue({
            el: '#app',
            data: window.BLTConfigCreateAppState ? window.BLTConfigCreateAppState() : {
                status: 'connecting',
                loaded: false,
                tab: 'commands',
                search: '',
                expanded: null,
                expandedConfig: null,
                activeProfile: 1,
                commands: [],
                rewards: [],
                commandHandlers: [],
                rewardHandlers: [],
                globalConfigs: [],
                simTesting: { userCount: 20, userStayTime: 60, intervalMinMS: 500, intervalMaxMS: 2000, init: [], use: [], isRunning: false },
                newInitAction: { type: 'Command', actionId: '', weight: 1.0 },
                newUseAction: { type: 'Command', actionId: '', weight: 1.0 },
                saveState: 'hidden',
                saveTimer: null,
                toast: { visible: false, message: '', error: false, timer: null },
            },
            computed: {
                statusText: function () {
                    return { connecting: 'Connecting…', connected: 'Live', disconnected: 'Offline' }[this.status] || '';
                },
                filteredCommands: function () {
                    var q = this.search.toLowerCase().trim();
                    if (!q) return this.commands;
                    return this.commands.filter(function (c) {
                        return (c.name && c.name.toLowerCase().indexOf(q) !== -1) ||
                               (c.help && c.help.toLowerCase().indexOf(q) !== -1) ||
                               (c.handler && c.handler.toLowerCase().indexOf(q) !== -1);
                    });
                },
                filteredRewards: function () {
                    var q = this.search.toLowerCase().trim();
                    if (!q) return this.rewards;
                    return this.rewards.filter(function (r) {
                        return (r.title && r.title.toLowerCase().indexOf(q) !== -1) ||
                               (r.prompt && r.prompt.toLowerCase().indexOf(q) !== -1) ||
                               (r.handler && r.handler.toLowerCase().indexOf(q) !== -1);
                    });
                },
            },
            methods: {
                handlerDisplayName: function (handlerId, type) {
                    var list = type === 'command' ? this.commandHandlers : this.rewardHandlers;
                    for (var i = 0; i < list.length; i++) {
                        if (list[i].id === handlerId) return list[i].displayName;
                    }
                    return handlerId || 'None';
                },

                toggleExpand: function (id) {
                    this.expanded = this.expanded === id ? null : id;
                },

                toggleConfigExpand: function (id) {
                    this.expandedConfig = this.expandedConfig === id ? null : id;
                },

                toggleEnabled: function (item) {
                    var self = this;
                    item.enabled = !item.enabled;
                        if (configHub && configHub.server) {
                        self.showSaving();
                        configHub.server.toggleAction(item.id, item.enabled)
                            .done(function (ok) {
                                if (ok) { self.showSaved(); }
                                else    { item.enabled = !item.enabled; self.showSaveError(); }
                            })
                            .fail(function () { item.enabled = !item.enabled; self.showSaveError(); });
                    }
                },

                updateCmd: function (cmd, field, value) {
                    var self = this;
                    var updates = {};
                    updates[field] = value;
                    cmd[field] = value;

                    if (configHub && configHub.server) {
                        self.showSaving();
                        configHub.server.updateCommand(cmd.id, updates)
                            .done(function (ok) {
                                if (ok) self.showSaved();
                                else    self.showSaveError();
                            })
                            .fail(function () { self.showSaveError(); });
                    }
                },

                updateReward: function (rw, field, value) {
                    var self = this;
                    var updates = {};
                    updates[field] = value;
                    rw[field] = value;

                    if (configHub && configHub.server) {
                        self.showSaving();
                        configHub.server.updateReward(rw.id, updates)
                            .done(function (ok) {
                                if (ok) self.showSaved();
                                else    self.showSaveError();
                            })
                            .fail(function () { self.showSaveError(); });
                    }
                },

                switchProfile: function (p) {
                    var self = this;
                    if (p === this.activeProfile) return;
                    if (configHub && configHub.server) {
                        self.showSaving();
                        configHub.server.switchProfile(p)
                            .done(function (ok) {
                                if (ok) {
                                    self.activeProfile = p;
                                    self.showToast('Switched to Profile ' + p, false);
                                    self.loadConfig();
                                } else {
                                    self.showSaveError();
                                }
                            })
                            .fail(function () { self.showSaveError(); });
                    }
                },

                groupedFields: function (gc) {
                    var cats = [];
                    var catMap = {};
                    var fields = gc.fields.slice().sort(function (a, b) { return (a.order || 0) - (b.order || 0); });
                    for (var i = 0; i < fields.length; i++) {
                        var f = fields[i];
                        var cat = f.category || '';
                        if (!catMap[cat]) {
                            catMap[cat] = { category: cat, fields: [] };
                            cats.push(catMap[cat]);
                        }
                        catMap[cat].fields.push(f);
                    }
                    return cats;
                },

                updateGlobalField: function (configId, field, value) {
                    var self = this;
                    field.value = value;
                    if (configHub && configHub.server) {
                        self.showSaving();
                        configHub.server.updateGlobalConfig(configId, field.key, value)
                            .done(function (ok) {
                                if (ok) self.showSaved();
                                else    self.showSaveError();
                            })
                            .fail(function () { self.showSaveError(); });
                    }
                },

                updateSimField: function (key, value) {
                    this.simTesting[key] = value;
                    this.saveSimConfig();
                },

                saveSimConfig: function () {
                    var self = this;
                    if (configHub && configHub.server) {
                        self.showSaving();
                        configHub.server.updateSimTesting(self.simTesting)
                            .done(function (ok) {
                                if (ok) self.showSaved();
                                else    self.showSaveError();
                            })
                            .fail(function () { self.showSaveError(); });
                    }
                },

                startSim: function () {
                    var self = this;
                    if (configHub && configHub.server) {
                        configHub.server.startSimulation()
                            .done(function (ok) {
                                if (ok) {
                                    self.simTesting.isRunning = true;
                                    self.showToast('Simulation started', false);
                                } else {
                                    self.showToast('Failed to start simulation', true);
                                }
                            })
                            .fail(function () { self.showToast('Failed to start simulation', true); });
                    }
                },

                stopSim: function () {
                    var self = this;
                    if (configHub && configHub.server) {
                        configHub.server.stopSimulation()
                            .done(function (ok) {
                                if (ok) {
                                    self.simTesting.isRunning = false;
                                    self.showToast('Simulation stopped', false);
                                } else {
                                    self.showToast('Failed to stop simulation', true);
                                }
                            })
                            .fail(function () { self.showToast('Failed to stop simulation', true); });
                    }
                },

                availableSimActions: function (type) {
                    if (type === 'Command') {
                        return this.commands.map(function (c) { return c.name; });
                    } else {
                        return this.rewards.map(function (r) { return r.title; });
                    }
                },

                addSimItem: function (list) {
                    var src = list === 'init' ? this.newInitAction : this.newUseAction;
                    if (!src.actionId) return;
                    var item = {
                        id: 'sim-' + Date.now() + '-' + Math.random().toString(36).substr(2, 5),
                        enabled: true,
                        type: src.type,
                        actionId: src.actionId,
                        args: '',
                        weight: src.weight || 1.0
                    };
                    if (!this.simTesting[list]) this.simTesting[list] = [];
                    this.simTesting[list].push(item);
                    src.actionId = '';
                    src.weight = 1.0;
                    this.saveSimConfig();
                },

                removeSimItem: function (list, idx) {
                    this.simTesting[list].splice(idx, 1);
                    this.saveSimConfig();
                },

                loadConfig: function () {
                    var self = this;
                    if (!configHub || !configHub.server) return;
                    configHub.server.getConfig()
                        .done(function (cfg) {
                            if (!cfg) return;
                            self.commands = cfg.commands || [];
                            self.rewards = cfg.rewards || [];
                            self.commandHandlers = cfg.commandHandlers || [];
                            self.rewardHandlers = cfg.rewardHandlers || [];
                            self.globalConfigs = cfg.globalConfigs || [];
                            if (cfg.simTesting) {
                                self.simTesting = cfg.simTesting;
                            }
                            self.activeProfile = cfg.activeProfile || 1;
                            self.loaded = true;
                            self.status = 'connected';
                            self.expanded = null;
                            self.expandedConfig = null;
                            self.search = '';
                        })
                        .fail(function () {
                            self.showToast('Failed to load configuration', true);
                        });
                },

                showSaving: function () {
                    if (this.saveTimer) clearTimeout(this.saveTimer);
                    this.saveState = 'saving';
                },

                showSaved: function () {
                    var self = this;
                    this.saveState = 'saved';
                    this.saveTimer = setTimeout(function () { self.saveState = 'hidden'; }, 2000);
                },

                showSaveError: function () {
                    var self = this;
                    this.saveState = 'error';
                    this.showToast('Failed to save — check game is running', true);
                    this.saveTimer = setTimeout(function () { self.saveState = 'hidden'; }, 4000);
                },

                showToast: function (message, isError) {
                    var self = this;
                    if (this.toast.timer) clearTimeout(this.toast.timer);
                    this.toast.message = message;
                    this.toast.error = !!isError;
                    this.toast.visible = true;
                    this.toast.timer = setTimeout(function () {
                        self.toast.visible = false;
                        self.toast.timer = null;
                    }, 3000);
                },
            },
        });

        window.configApp = app;
        return app;
    }

    function startPage() {
        var app = createApp();
        window.toastVm = new Vue({
            el: '#toast',
            data: app.$data,
        });

        if (typeof $.connection !== 'undefined' && typeof $.connection.configHub !== 'undefined') {
            $.connection.hub.url = window.location.origin + '/signalr';

            $.connection.hub.reconnecting(function () { app.status = 'disconnected'; });
            $.connection.hub.reconnected(function () { app.loadConfig(); });
            $.connection.hub.disconnected(function () { app.status = 'disconnected'; });

            configHub = window.configHub = $.connection.configHub;

            $.connection.hub.start()
                .done(function () {
                    app.status = 'connected';
                    app.loadConfig();
                })
                .fail(function () {
                    app.status = 'disconnected';
                });
        } else {
            app.status = 'connected';
            if (window.BLTConfigApplyDemoData) {
                window.BLTConfigApplyDemoData(app, window.BLTConfigDemoData);
            }
            app.activeProfile = 1;
            app.loaded = true;
        }
    }

    window.BLTConfigCreatePageApp = createApp;
    window.BLTConfigStartPage = startPage;
    startPage();
}());
