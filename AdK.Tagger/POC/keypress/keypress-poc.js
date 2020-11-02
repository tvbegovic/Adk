
angular.module('poc-app', [])
  .controller('keypressPocCtrl', ['$scope', function($scope) {
    $scope.hello = 'ello';
    $scope.color = 'gray';

    Mousetrap.bind(['ctrl+p'], function(e) {
      console.log(e.keyCode);
      changeColor('red');
      if (e.preventDefault) {
        e.preventDefault();
      } else {
        e.returnValue = false;
      }
    });

    Mousetrap.bind(['ctrl+d'], function(e) {
      changeColor('green');
      if (e.preventDefault) {
        e.preventDefault();
      } else {
        // internet explorer
        e.returnValue = false;
      }
    });

    Mousetrap.bind(['ctrl+o'], function(e) {
      changeColor('blue');
      if (e.preventDefault) {
        e.preventDefault();
      } else {
        // internet explorer
        e.returnValue = false;
      }
    });

    Mousetrap.bind(['ctrl+t'], function(e) {
      console.log('ctrl tab is clicked');
      changeColor('green');
      if (e.preventDefault) {
        e.preventDefault();
      } else {
        // internet explorer
        e.returnValue = false;
      }
    });


    Mousetrap.bind(['ctrl+w'], function(e) {
      changeColor('black');
      if (e.preventDefault) {
        e.preventDefault();
      } else {
        // internet explorer
        e.returnValue = false;
      }
    });

    function changeColor(color) {
      $scope.$apply(function() {
        $scope.color = color;
      });
    }

  }]);
