// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

import Player from './player.js'

const player = new Player()

// Enable Bootstrap popovers
const popoverTriggerList = document.querySelectorAll('[data-bs-toggle="popover"]')
const _ = [...popoverTriggerList].map(popoverTriggerEl => new bootstrap.Popover(popoverTriggerEl))

if ($('.loader').length > 0) {
    $('main').load('/Library')
}

$('main').on('click', '.library .media', (e) => {
    if (!player) return
    const $media = $(e.currentTarget)
    const status = $media.data('status')
    switch (status) {
        case 'playable':
            player.cast(`${$('.library').data('host')}library?handler=mediastream&guid=${$media.data('id')}`, {
                title: $media.data('title')
            })
            break;
        case 'unplayable':
            $.post(`conversion?handler=startconversion`, { guid: $media.data('id') })
            break;
        default:
            console.warn(`No action defined for media state: ${status}`)
    }
});

// Check conversion queue state
function checkQueueState() {
    $.get('/conversion?handler=state', (data, status) => {
        console.log(data)
        if (status === "success") setTimeout(checkQueueState, 300)
    })
}
const $conversionPlaceholder = $('.conversion-placeholder')
if ($conversionPlaceholder.length > 0) checkQueueState()
