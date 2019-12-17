// modified from https://github.com/squidfunk/mkdocs-material/issues/315
(function (document) {
    function clipboard_init() {
        var charts = document.querySelectorAll("code.JSON, code.python, code.bash"),
            arr = [],
            i, j, maxItem, code, btn, el;

        for (i = 0, maxItem = charts.length; i < maxItem; i++)
        {
            var parent = charts[i].closest("pre")
            if (parent != null)
            {
                arr.push(parent)
            }
        }

        // Make sure we are dealing with an array
        // for(i = 0, maxItem = charts.length; i < maxItem; i++) arr.push(charts[i]);

        // Find the UML source element and get the text
        for (i = 0, maxItem = arr.length; i < maxItem; i++) {
            el = arr[i];
            code = el.childNodes[0];

            code.id = "hl_code" + i.toString();
            btn = document.createElement('button');
            btn.appendChild(document.createTextNode('Copy'));
            btn.setAttribute("class", "copyButton");
            btn.setAttribute("data-clipboard-target", "#hl_code" + i.toString());
            el.insertBefore(btn, code);
        }
        new Clipboard('.copyButton');
    };

    function onReady(fn) {
        if (document.addEventListener) {
            document.addEventListener('DOMContentLoaded', fn);
        } else {
            document.attachEvent('onreadystatechange', function() {
                if (document.readyState === 'interactive')
                    fn();
            });
        }
    }

    onReady(function(){
        clipboard_init();
    });
})(document);