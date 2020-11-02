angular.module('app')
  .controller('mediaHouseCtrl', ['$scope', 'Service', 'confirmPopup', 'focus', 'debounce', 'MyChannels', function($scope, Service, confirmPopup, focus, debounce, MyChannels) {
    $scope.current = {
      dayPartSet: null,
      newSetName: '',
      part: null,
      editingPart: false,
      hour: null
    };

    //This is used in reports filter, make sure we use latest list here not cached version
    MyChannels.refreshChannels();

	  Service('GetUserDayParts', { isV2: false }).then(function(dayPartSets) {
      $scope.dayPartSets = dayPartSets;
      if (dayPartSets.length) {
        $scope.current.dayPartSet = dayPartSets[0];
        if ($scope.current.dayPartSet.Parts.length) {
          $scope.current.part = $scope.current.dayPartSet.Parts[0];
        }
      }
    });
    $scope.days = [{ id: 1, name: 'Mon' }, { id: 2, name: 'Tue' }, { id: 3, name: 'Wed' }, { id: 4, name: 'Thu' }, { id: 5, name: 'Fri' }, { id: 6, name: 'Sat' }, { id: 0, name: 'Sun' }];
    $scope.hours = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23];
    function getPart(day, hour) {
      var found = null;
      if ($scope.current.dayPartSet) {
        $scope.current.dayPartSet.Parts.some(function(part) {
          var dh = getDayHour(part, day, hour);
          if (dh != null) {
            found = part;
          }
          return found;
        });
      }
      return found;
    }
    function getDayHour(part, day, hour) {
      var found = null;
      part.Hours.some(function(dh) {
        if (isInHour(dh, day, hour))
          found = dh;
        return found;
      });
      return found;
    }
    function isInPart(part, day, hour) {
      return part.Hours.some(function(dh) {
        return isInHour(dh, day, hour);
      });
    }
    function isInHour(dh, day, hour) {
      return dh.Day === day && dh.Hour === hour;
    }

    $scope.hasPart = function(day, hour) {
      var part = getPart(day, hour);
      return part != null;
    };
    $scope.partName = function(day, hour) {
      var part = getPart(day, hour);
      return part != null ? part.Name : null;
    };
    $scope.hourStyle = function(day, hour) {
      var part = getPart(day, hour);
      if (part != null) {
        var index = $scope.current.dayPartSet.Parts.indexOf(part);
        var dh = getDayHour(part, day, hour);
        if (dh != null) {
          var classes = {
            selected: part === $scope.current.part
          };
          classes['color-' + index] = true;
          return classes;
        }
      }
      return null;
    };
    $scope.hourClicked = function(ev, day, hour) {
      var part = getPart(day, hour);
      if (!$scope.current.part && !part) return;
      if (part) {
        if ($scope.current.part === part) { // Hour of current part clicked: remove it
          _.pull(part.Hours, getDayHour(part, day, hour));
          debouncedUpdatePart(part);
        } else
          $scope.current.part = part; // Click on another part: select that part
      } else { // Empty hour selected: add it to current part
        $scope.current.part.Hours.push({
          DayPartId: $scope.current.part.Id,
          Day: day,
          Hour: hour
        });
        debouncedUpdatePart($scope.current.part);
      }
    };
    $scope.partClicked = function(part) {
      if ($scope.current.part === part)
        editPartName(part);
      else {
        $scope.current.editingPart = false;
        $scope.current.part = part;
      }
    }
    function editPartName(part) {
      var index = $scope.current.dayPartSet.Parts.indexOf(part);
      $scope.current.editingPart = true;
      focus('part' + index);
    }
    function updatePart(part) {
      $scope.current.editingPart = false;
      Service('UpdateDayPart', { dayPart: part }, { backgroundLoad: true });
    }
    var debouncedUpdatePart = debounce(500, updatePart);
    $scope.updatePartName = debouncedUpdatePart;

    $scope.deleteSet = function(ev, set, index) {
      ev.stopPropagation();
      $scope.current.dayPartSet = set;
      confirmPopup.open("Delete Day Part Set", null, "Do you want to delete '" + set.Name + "' and all its Day Parts?").then(function() {
        Service('DeleteDayPartSet', { setId: set.Id }).then(function(deleted) {
          if (deleted) {
            $scope.dayPartSets.splice(index, 1);
            $scope.current.dayPartSet = null;
          }
        });
      });
    };
    $scope.createSet = function(ev) {
      ev.stopPropagation();
      Service('CreateDayPartSet', { name: $scope.current.newSetName }).then(function(dayPartSet) {
        $scope.dayPartSets.push(dayPartSet);
        $scope.current.newSetName = '';
        $scope.current.dayPartSet = dayPartSet;
      });
    };
    $scope.deletePart = function() {
      confirmPopup.open("Delete Day Part", null, "Do you want to delete the part '" + $scope.current.part.Name + "'?").then(function() {
        //var index = $scope.current.dayPartSet.Parts.indexOf(part);
        Service('DeleteDayPart', { setId: $scope.current.dayPartSet.Id, partId: $scope.current.part.Id }, { backgroundLoad: true }).then(function(deleted) {
          if (deleted) {
            _.pull($scope.current.dayPartSet.Parts, $scope.current.part);
            //$scope.current.dayPartSet.Parts.splice(index, 1);
            $scope.current.part = null;
          }
        });
      });
    };
    $scope.addPart = function() {
      var part = { Hours: [] };
      Service('CreateDayPart', { setId: $scope.current.dayPartSet.Id, dayPart: part }, { backgroundLoad: true }).then(function(dayPart) {
        $scope.current.dayPartSet.Parts.push(dayPart);
        $scope.current.part = dayPart;
        editPartName(dayPart);
      });
    };
  }]);
