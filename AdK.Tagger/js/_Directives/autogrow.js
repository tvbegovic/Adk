angular.module('app')
    .directive("textarea", function () {
        return {
            restrict: "E",
            require: "ngModel",
            link: function (scope, element, attributes) {
                if (!element.hasClass("autogrow")) {
                    return;
                }

                var minHeight = parseInt(window.getComputedStyle(element[0]).getPropertyValue("min-height")) || 0;

                element.on("input", function (evt) {
                    element.css({
                        paddingTop: 0,
                        height: 0,
                        minHeight: 0
                    });

                    var contentHeight = this.scrollHeight;
                    var borderHeight = this.offsetHeight;

                    element.css({
                        paddingTop: ~~Math.max(0, minHeight - contentHeight) / 2 + "px",
                        minHeight: null,
                        height: contentHeight + borderHeight + "px"
                    });
                });

                scope.$watch(attributes.ngModel, trigger);

                trigger();

                function trigger() {
                    setTimeout(element.triggerHandler.bind(element, "input"), 1);
                }
            }
        };
    });

