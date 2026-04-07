window.BLTConfigCreateAppState = function () {
    return {
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
    };
};

window.BLTConfigApplyDemoData = function (app, demoData) {
    if (!app || !demoData) {
        return;
    }

    app.commandHandlers = demoData.commandHandlers || [];
    app.rewardHandlers = demoData.rewardHandlers || [];
    app.commands = demoData.commands || [];
    app.rewards = demoData.rewards || [];
    app.globalConfigs = demoData.globalConfigs || [];
    app.simTesting = demoData.simTesting || app.simTesting;
};
