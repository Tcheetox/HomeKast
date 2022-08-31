// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

import Player from './player.js'

const player = new Player()

if ($('.loader').length > 0) {
    $('main').load('/Library')
}

$('main').on('click', '.library .media', (e) => {
    if (!player) return
    const media = $(e.currentTarget)
    const url = `${$('.library').data('host')}library?handler=mediastream&guid=${media.data('id')}`
    console.log(url)
    player.cast(url, {
        title: media.data('title')
    })
});

const $conversionPlaceholder = $('.conversion-placeholder')
const updateConversion = () => {
    console.log("YOLO")
}
if ($conversionPlaceholder.length > 0)
    setInterval(updateConversion, 500)
