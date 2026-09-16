$(document).ready(function(){
    $(".status").click(function(){
        num = $(this).parents('tr').find('td.so').text();
        // alert(num);
    })
})