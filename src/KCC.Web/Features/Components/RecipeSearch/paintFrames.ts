// Work measured and applied inside a frame: a change made between frames is compared with the last painted one, so
// a jump to the bottom landing before the next frame would see the footer move.
export function paintFrames(run: () => void) {
  let frame = 0

  const nextFrame = () => {
    frame ||= requestAnimationFrame(() => {
      frame = 0
      run()
    })
  }

  // A page that lands while the end of the results is on screen moves what follows it. Space added in that same
  // frame would add to the distance it is seen to move, so it waits for the landing to be painted, and a frame a
  // scroll had already asked for gives way to that wait.
  const afterPaint = () => {
    cancelAnimationFrame(frame)
    frame = requestAnimationFrame(() => {
      frame = requestAnimationFrame(() => {
        frame = 0
        run()
      })
    })
  }

  const cancel = () => cancelAnimationFrame(frame)

  return { nextFrame, afterPaint, cancel }
}
