angular.module('app')
.controller('settingsCtrl', ['$scope', 'Service', 'emailComposer', function ($scope, Service, emailComposer) {

    $scope.DummyDate = new Date(2017, 3, 21);

    Service('GetSettings').then(function (config) {
        config.Transcript.AutoTranscribeStartDate = moment(config.Transcript.AutoTranscribeStartDate).format('YYYY-MM-DD');
        $scope.Global = config.Global;
        $scope.Mail = config.Mail;
        $scope.Transcript = config.Transcript;
    });
    $scope.editTranscriptDoneNotificationEmail = function () {
        emailComposer.open(
        "Notification on finished transcript", null,
        $scope.Transcript.TranscriptDoneNotificationSubject,
        $scope.Transcript.TranscriptDoneNotificationBody,
        ['transcriptDateTime', 'transcriberName', 'sampleUrl', 'transcriptText', 'performance', 'performanceGrade']
        ).then(function (template) {
            $scope.Transcript.TranscriptDoneNotificationSubject = template.subject;
            $scope.Transcript.TranscriptDoneNotificationBody = template.body;
        });
    };
    $scope.editOrderNotificationEmail = function () {
        emailComposer.open(
        "Notification on ordered transcripts", null,
        $scope.Transcript.OrderNotificationSubject,
        $scope.Transcript.OrderNotificationBody,
        ['transcriberName', 'transcriptUrl']
        ).then(function (template) {
            $scope.Transcript.OrderNotificationSubject = template.subject;
            $scope.Transcript.OrderNotificationBody = template.body;
        });
    };
    $scope.editAwaitingTranscriptsNotificationEmail = function () {
        emailComposer.open(
        "Notify that samples are waiting to be transcribed", null,
        $scope.Transcript.AwaitingTranscriptsNotificationSubject,
        $scope.Transcript.AwaitingTranscriptsNotificationBody,
        ['transcriberName', 'transcriptUrl', 'transcriptCount']
        ).then(function (template) {
            $scope.Transcript.AwaitingTranscriptsNotificationSubject = template.subject;
            $scope.Transcript.AwaitingTranscriptsNotificationBody = template.body;
        });
    };
    $scope.saveGlobal = function () {
        localStorage.UserDateFormat = $scope.Global.UserDateFormat;
        moment.locale($scope.Global.Locale);
        Service('SaveGlobalSettings', { config: $scope.Global });
    };
    $scope.saveMail = function () {
        Service('SaveMailSettings', { config: $scope.Mail }).then(function () {
            $scope.Mail.CredentialPassword = "***";
        });
    };
    $scope.dateChanged = function () {
        setTimeout(function () { // delaying as dateChanged fires before actual values are changed
            if ($scope.Transcript.AutoTranscribeStartDate)
                $scope.Transcript.AutoTranscribeStartDate = moment($scope.Transcript.AutoTranscribeStartDate).format('YYYY-MM-DD');
        });
    };
    $scope.saveTranscript = function () {
        Service('SaveTranscriptSettings', {
            config: $scope.Transcript
        });
	};

	$scope.unlockSongs = function () {
		Service('UnlockTranscribedSongs');
	}
}]);
