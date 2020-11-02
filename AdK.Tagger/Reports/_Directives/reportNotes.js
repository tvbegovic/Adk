angular.module('app.reports')
  .directive('reportNotes', ['$modal', '$timeout', 'Service', function($modal, $timeout, Service) {
    return {
      restrict: 'AE',
      scope: {
        noteId: '@'
      },
      templateUrl: 'Reports/_Directives/reportNotes.html',
      link: function(scope) {
        var backgroundLoad = { backgroundLoad: true };
        scope.noteType = {
          notes: {
            key: 'notes'
          },
          questions: {
            key: 'questions'
          }
        };


        function getNotes(noteType) {
          Service('GetNoteByKey', { key: getNoteKey(noteType.key) }, backgroundLoad)
            .then(function(note) {
              noteType.haveNotes = note && note.Content && note.Content.trim().length;
            });
        }

        getNotes(scope.noteType.notes);
        getNotes(scope.noteType.questions)

        function getNoteKey(noteType) {
          return scope.noteId + '_' + noteType;
        }

        scope.openNoteModal = function(noteType) {
          var noteKey = getNoteKey(noteType);

          $modal.open({
            templateUrl: 'notesModal.html',
            size: 'lg',
            backdrop: 'static',
            keyboard: false,
            controller: ['$scope', '$modalInstance', function($scope, $modalInstance) {
              $scope.loading = true;
              $scope.focusEditor = true;

              Service('GetNoteByKey', { key: noteKey }, backgroundLoad)
                .then(function(note) {
                  $scope.note = note;
                })
                .catch(function() { $scope.showErrorMsg = true; })
                .finally(function() { $scope.loading = false; });


              var update = _.debounce(function() {
                var request = {
                  noteKey: $scope.note.Key,
                  noteContent: $scope.note.Content
                };

                Service('InserOrUpdateNote', request, backgroundLoad)
                  .then(function() {
                    $scope.successUpdate = true;
                    $timeout(function() {
                      $scope.successUpdate = false;
                    }, 2000);
                  })
                  .catch(function() { $scope.showErrorMsg = true; })
                  .finally(function() { $scope.loading = false; });
              }, 200);

              $scope.updateNote = function() {
                $scope.loading = true;
                $scope.showErrorMsg = false;
                update();
              };

              $scope.close = function() { $modalInstance.close($scope.markets); };

            }]
          });
        };

      }
    };
  }]);
